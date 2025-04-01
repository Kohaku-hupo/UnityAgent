using UnityEngine;
using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

public class MongoDBManager : MonoBehaviour
{
    private static MongoDBManager instance;
    private IMongoDatabase database;
    private IMongoDatabase shortTermDB;  // 短期记忆数据库
    private IMongoDatabase longTermDB;   // 长期记忆数据库
    private MongoClient client;
    private const string CONNECTION_STRING = "mongodb://127.0.0.1:27017/?serverSelectionTimeoutMS=5000&connectTimeoutMS=10000";
    private const string DATABASE_NAME = "UnityAgentDB";
    private const string SHORT_TERM_DB = "ShortTermMemoryDB";  // 短期记忆数据库名
    private const string LONG_TERM_DB = "LongTermMemoryDB";    // 长期记忆数据库名
    private bool isInitialized = false;

    // 定义集合名称
    private const string COLLECTION_MODEL_RESPONSES = "model_responses";
    private const string COLLECTION_MEMORY = "memory";         // 用于存储记忆的通用集合名
    private const string COLLECTION_ENVIRONMENT = "environment";
    private const string COLLECTION_TASKS = "priority_tasks";
    private const string COLLECTION_CHAT = "chat_history";
    
    public static MongoDBManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("MongoDBManager");
                instance = go.AddComponent<MongoDBManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        try
        {
            Debug.Log("Attempting to connect to MongoDB...");
            
            var settings = MongoClientSettings.FromUrl(new MongoUrl(CONNECTION_STRING));
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
            settings.ConnectTimeout = TimeSpan.FromSeconds(10);
            settings.DirectConnection = true;
            
            Debug.Log($"Connecting to MongoDB with settings: DirectConnection={settings.DirectConnection}, ServerSelectionTimeout={settings.ServerSelectionTimeout}, ConnectTimeout={settings.ConnectTimeout}");
            
            client = new MongoClient(settings);
            database = client.GetDatabase(DATABASE_NAME);
            shortTermDB = client.GetDatabase(SHORT_TERM_DB);
            longTermDB = client.GetDatabase(LONG_TERM_DB);
            
            // 测试连接
            database.RunCommand<BsonDocument>(new BsonDocument("ping", 1));
            shortTermDB.RunCommand<BsonDocument>(new BsonDocument("ping", 1));
            longTermDB.RunCommand<BsonDocument>(new BsonDocument("ping", 1));
            
            // 确保所有需要的集合都存在
            EnsureCollectionExists(database, COLLECTION_MODEL_RESPONSES);
            EnsureCollectionExists(database, COLLECTION_ENVIRONMENT);
            EnsureCollectionExists(database, COLLECTION_TASKS);
            EnsureCollectionExists(database, COLLECTION_CHAT);
            EnsureCollectionExists(shortTermDB, COLLECTION_MEMORY);
            EnsureCollectionExists(longTermDB, COLLECTION_MEMORY);
            
            isInitialized = true;
            Debug.Log("MongoDB connections established successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to connect to MongoDB: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            isInitialized = false;
        }
    }

    private void EnsureCollectionExists(IMongoDatabase db, string collectionName)
    {
        if (!CollectionExists(db, collectionName))
        {
            db.CreateCollection(collectionName);
            Debug.Log($"Created collection: {collectionName} in database: {db.DatabaseNamespace.DatabaseName}");
        }
    }

    private bool CollectionExists(IMongoDatabase db, string collectionName)
    {
        var filter = new BsonDocument("name", collectionName);
        var collections = db.ListCollections(new ListCollectionsOptions { Filter = filter });
        return collections.Any();
    }

    public async Task SaveModelResponse(string fullResponse, object parsedContent)
    {
        if (!isInitialized)
        {
            Debug.LogError("MongoDB is not initialized. Attempting to reinitialize...");
            InitializeDatabase();
            if (!isInitialized)
            {
                throw new Exception("MongoDB failed to initialize");
            }
        }

        try
        {
            var collection = database.GetCollection<BsonDocument>(COLLECTION_MODEL_RESPONSES);
            
            // 将parsedContent转换为JObject以便更好地处理
            string parsedContentStr = JsonConvert.SerializeObject(parsedContent);
            JObject parsedContentObj = JObject.Parse(parsedContentStr);
            
            // 创建BSON文档
            var document = new BsonDocument
            {
                { "timestamp", DateTime.UtcNow }
            };

            // 只保存modelResponse部分
            if (parsedContentObj["metadata"] != null && 
                parsedContentObj["metadata"]["modelResponse"] != null)
            {
                var modelResponse = parsedContentObj["metadata"]["modelResponse"];
                document.Add("modelResponse", BsonDocument.Parse(modelResponse.ToString()));
            }
            
            // 如果有解析后的内容，也保存
            if (parsedContentObj["parsedContent"] != null)
            {
                document.Add("parsedContent", BsonDocument.Parse(parsedContentObj["parsedContent"].ToString()));
            }
            else if (parsedContentObj["rawContent"] != null)
            {
                document.Add("rawContent", parsedContentObj["rawContent"].ToString());
            }

            Debug.Log($"Saving document to MongoDB: {document.ToString()}");
            await collection.InsertOneAsync(document);
            Debug.Log("Response saved to MongoDB successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save response to MongoDB: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            throw;
        }
    }

    public async Task<BsonDocument> GetResponseById(string id)
    {
        if (!isInitialized)
        {
            Debug.LogError("MongoDB is not initialized");
            return null;
        }

        try
        {
            var collection = database.GetCollection<BsonDocument>(COLLECTION_MODEL_RESPONSES);
            var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(id));
            return await collection.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to retrieve response from MongoDB: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            return null;
        }
    }

    public async Task<IAsyncCursor<BsonDocument>> GetAllResponses()
    {
        if (!isInitialized)
        {
            Debug.LogError("MongoDB is not initialized");
            return null;
        }

        try
        {
            var collection = database.GetCollection<BsonDocument>(COLLECTION_MODEL_RESPONSES);
            return await collection.FindAsync(new BsonDocument());
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to retrieve responses from MongoDB: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            return null;
        }
    }

    // 保存记忆（短期或长期）
    public async Task SaveMemory(string memoryType, object memoryContent)
    {
        if (string.IsNullOrEmpty(memoryType) || memoryContent == null) return;

        try
        {
            // 根据记忆类型选择数据库
            IMongoDatabase targetDB = memoryType.ToLower() == "shortterm" ? shortTermDB : longTermDB;
            var collection = targetDB.GetCollection<BsonDocument>(COLLECTION_MEMORY);
            
            // 将内容转换为JToken以便处理数组或对象
            string contentStr = JsonConvert.SerializeObject(memoryContent);
            JToken contentToken = JToken.Parse(contentStr);
            
            var document = new BsonDocument
            {
                { "timestamp", DateTime.UtcNow }
            };

            // 根据内容类型添加到文档
            if (contentToken.Type == JTokenType.Array)
            {
                // 如果是数组，创建BsonArray并添加元素
                var bsonArray = new BsonArray();
                foreach (var item in contentToken)
                {
                    if (item.Type == JTokenType.Object)
                    {
                        bsonArray.Add(BsonDocument.Parse(item.ToString()));
                    }
                    else
                    {
                        bsonArray.Add(BsonValue.Create(item.ToObject<object>()));
                    }
                }
                document.Add("content", bsonArray);
            }
            else
            {
                // 如果是对象或其他类型，添加为BsonDocument
                document.Add("content", BsonDocument.Parse(contentStr));
            }

            await collection.InsertOneAsync(document);
            Debug.Log($"{memoryType} memory saved successfully to {targetDB.DatabaseNamespace.DatabaseName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save {memoryType} memory: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            throw;
        }
    }

    // 保存环境更新
    public async Task SaveEnvironmentUpdate(object environmentData)
    {
        if (environmentData == null) return;

        try
        {
            var collection = database.GetCollection<BsonDocument>(COLLECTION_ENVIRONMENT);
            
            // 将内容转换为JToken以便处理数组或对象
            string contentStr = JsonConvert.SerializeObject(environmentData);
            JToken contentToken = JToken.Parse(contentStr);
            
            var document = new BsonDocument
            {
                { "timestamp", DateTime.UtcNow }
            };

            // 根据内容类型添加到文档
            if (contentToken.Type == JTokenType.Array)
            {
                // 如果是数组，创建BsonArray并添加元素
                var bsonArray = new BsonArray();
                foreach (var item in contentToken)
                {
                    if (item.Type == JTokenType.Object)
                    {
                        bsonArray.Add(BsonDocument.Parse(item.ToString()));
                    }
                    else
                    {
                        bsonArray.Add(BsonValue.Create(item.ToObject<object>()));
                    }
                }
                document.Add("environment", bsonArray);
            }
            else
            {
                // 如果是对象或其他类型，添加为BsonDocument
                document.Add("environment", BsonDocument.Parse(contentStr));
            }

            await collection.InsertOneAsync(document);
            Debug.Log("Environment update saved successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save environment update: {e.Message}");
            throw;
        }
    }

    // 保存任务列表更新
    public async Task SaveTaskListUpdate(object taskList)
    {
        if (taskList == null) return;

        try
        {
            var collection = database.GetCollection<BsonDocument>(COLLECTION_TASKS);
            
            // 将内容转换为JToken以便处理数组或对象
            string contentStr = JsonConvert.SerializeObject(taskList);
            JToken contentToken = JToken.Parse(contentStr);
            
            var document = new BsonDocument
            {
                { "timestamp", DateTime.UtcNow }
            };

            // 根据内容类型添加到文档
            if (contentToken.Type == JTokenType.Array)
            {
                // 如果是数组，创建BsonArray并添加元素
                var bsonArray = new BsonArray();
                foreach (var item in contentToken)
                {
                    if (item.Type == JTokenType.Object)
                    {
                        bsonArray.Add(BsonDocument.Parse(item.ToString()));
                    }
                    else
                    {
                        bsonArray.Add(BsonValue.Create(item.ToObject<object>()));
                    }
                }
                document.Add("tasks", bsonArray);
            }
            else
            {
                // 如果是对象或其他类型，添加为BsonDocument
                document.Add("tasks", BsonDocument.Parse(contentStr));
            }

            await collection.InsertOneAsync(document);
            Debug.Log("Task list update saved successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save task list update: {e.Message}");
            throw;
        }
    }

    // 保存对话记录
    public async Task SaveChatHistory(string userContent, string responseToUser)
    {
        // 如果用户内容和响应都为空，则不保存
        if (string.IsNullOrEmpty(userContent) && string.IsNullOrEmpty(responseToUser)) return;

        try
        {
            var collection = database.GetCollection<BsonDocument>(COLLECTION_CHAT);
            var document = new BsonDocument
            {
                { "timestamp", DateTime.UtcNow },
                { "userContent", userContent ?? "none" },
                { "responseToUser", responseToUser ?? "none" }
            };

            await collection.InsertOneAsync(document);
            Debug.Log("Chat history saved successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save chat history: {e.Message}");
            throw;
        }
    }

    // 获取最近的记忆
    public async Task<BsonDocument> GetRecentMemory(string memoryType, int limit = 10)
    {
        try
        {
            // 根据记忆类型选择数据库
            IMongoDatabase targetDB = memoryType.ToLower() == "shortterm" ? shortTermDB : longTermDB;
            var collection = targetDB.GetCollection<BsonDocument>(COLLECTION_MEMORY);
            var sort = Builders<BsonDocument>.Sort.Descending("timestamp");
            return await collection.Find(new BsonDocument()).Sort(sort).Limit(limit).FirstOrDefaultAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to retrieve {memoryType} memory: {e.Message}");
            return null;
        }
    }

    // 获取最新的环境状态
    public async Task<BsonDocument> GetLatestEnvironment()
    {
        try
        {
            var collection = database.GetCollection<BsonDocument>(COLLECTION_ENVIRONMENT);
            var sort = Builders<BsonDocument>.Sort.Descending("timestamp");
            return await collection.Find(new BsonDocument()).Sort(sort).FirstOrDefaultAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to retrieve latest environment: {e.Message}");
            return null;
        }
    }

    // 获取最新的任务列表
    public async Task<BsonDocument> GetLatestTaskList()
    {
        try
        {
            var collection = database.GetCollection<BsonDocument>(COLLECTION_TASKS);
            var sort = Builders<BsonDocument>.Sort.Descending("timestamp");
            return await collection.Find(new BsonDocument()).Sort(sort).FirstOrDefaultAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to retrieve latest task list: {e.Message}");
            return null;
        }
    }

    // 获取最近的对话历史
    public async Task<List<BsonDocument>> GetRecentChatHistory(int limit = 10)
    {
        try
        {
            var collection = database.GetCollection<BsonDocument>(COLLECTION_CHAT);
            var sort = Builders<BsonDocument>.Sort.Descending("timestamp");
            return await collection.Find(new BsonDocument())
                                 .Sort(sort)
                                 .Limit(limit)
                                 .ToListAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to retrieve chat history: {e.Message}");
            return null;
        }
    }

    // 获取所有短期记忆
    public async Task<List<BsonDocument>> GetAllShortTermMemories(int limit = 100)
    {
        try
        {
            var collection = shortTermDB.GetCollection<BsonDocument>(COLLECTION_MEMORY);
            var sort = Builders<BsonDocument>.Sort.Descending("timestamp");
            return await collection.Find(new BsonDocument())
                                 .Sort(sort)
                                 .Limit(limit)
                                 .ToListAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to retrieve short term memories: {e.Message}");
            return null;
        }
    }

    // 获取所有长期记忆
    public async Task<List<BsonDocument>> GetAllLongTermMemories(int limit = 100)
    {
        try
        {
            var collection = longTermDB.GetCollection<BsonDocument>(COLLECTION_MEMORY);
            var sort = Builders<BsonDocument>.Sort.Descending("timestamp");
            return await collection.Find(new BsonDocument())
                                 .Sort(sort)
                                 .Limit(limit)
                                 .ToListAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to retrieve long term memories: {e.Message}");
            return null;
        }
    }
} 