namespace Countries.DomainModel {

    #region Usings
    using Extensions;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Validation;
    using System.Data.Entity.Core.Objects;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Security.Principal;
    using System.Text;
    using System.Threading;
    using System.Transactions;
    using System.Diagnostics;
    using System.Configuration;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Core.Mapping;
    #endregion

    /// <summary>
    /// Wrapper class for CountriesEntities. Most customizations/extensions are implemented here instead of CountriesEntities.
    /// </summary>
    public class CountriesEntityManager : CountriesEntities, ICountriesEntityManager {

        #region Members
        /// <summary>
        /// TransactionOptions for large numbers of changes
        /// </summary>
        private TransactionOptions BulkTransactionOptions { get; set; }

        /// <summary>
        /// Gets the connection string for the correct environment (PROD, TEST, Local)
        /// </summary>
        private static string ConnectionString {
            get {
                if (string.IsNullOrWhiteSpace(connectionString)) {
                    string connectionStringName = "CountriesEntities";

                    foreach (ConnectionStringSettings cs in ConfigurationManager.ConnectionStrings) {
                        if (string.Equals(cs.Name, connectionStringName, StringComparison.OrdinalIgnoreCase)) {
                            //Debug.WriteLine("[ThreadId: {0}] {1} Connection string name: {2} value: {3}",
                            //    Thread.CurrentThread.ManagedThreadId, ObjectExtensions.CurrentMethodName(), cs.Name, cs.ConnectionString);
                            connectionString = cs.ToString();
                            break;
                        }
                    }
                }

                return connectionString;
            }
        }
        private static string connectionString;

        private static readonly Dictionary<System.Transactions.IsolationLevel, string> IsolationLevelCommands;

        /// <summary>
        /// The last error that occurred.
        /// </summary>
        public Exception LastError { get; private set; }

        /// <summary>
        /// Specifies if detailed information is logged about each entity when SaveChanges is called.
        /// </summary>
        private static readonly bool LogChangesDuringSave;

        /// <summary>
        /// Used to validate that this instance is not used by other processes/threads
        /// </summary>
        private readonly int ProcessId;

        /// <summary>
        /// Timeout in seconds For database commands.
        /// </summary>
        private static int SqlCommandTimeoutSeconds { get; set; }

        /// <summary>
        /// Minimum timeout in seconds For database commands. The default for SqlCommand is 30 seconds.
        /// </summary>
        private static int SqlCommandTimeoutSecondsMinimum { get; set; }

        /// <summary>
        /// The maximum value for a SQL time column
        /// </summary>
        public static readonly TimeSpan SqlTimeMaxValue = TimeSpan.FromHours(24).Subtract(TimeSpan.FromTicks(1));

        /// <summary>
        /// Used to validate that this instance is not used by other processes/threads
        /// </summary>
        private readonly int ThreadId;

        /// <summary>
        /// The default TransactionOptions
        /// </summary>
        private TransactionOptions TransactionOptions { get; set; }

        /// <summary>
        /// The default TransactionScopeOption (Required/Suppressed).
        /// https://blogs.msdn.microsoft.com/dbrowne/2010/06/03/using-new-transactionscope-considered-harmful/
        /// </summary>
        private static TransactionScopeOption TransactionScopeOptions { get; set; }

        /// <summary>
        /// The number of retries allowed if an update fails due to timeout or deadlock.
        /// </summary>
        private static int UpdateRetryLimit { get; set; }

        /// <summary>
        /// The amount of time to wait before a retry if an update fails due to timeout or deadlock.
        /// </summary>
        private static TimeSpan UpdateRetryInterval { get; set; }

        /// <summary>
        /// The identity associated with the operations performed by the EntityManager
        /// </summary>
        internal IIdentity UserIdentity { get; private set; }

        #region Collections
        private List<string> ChangeTrackerDetails { get; set; }

        /// <summary>
        /// Returns a list of all validation errors as DbEntityValidationResult objects
        /// </summary>
        public IReadOnlyList<DbEntityValidationResult> ValidationSummary { get { return this.validationSummary; } }
        private List<DbEntityValidationResult> validationSummary;
        #endregion
        #endregion

        #region Constructors
        static CountriesEntityManager() {
            IsolationLevelCommands = new Dictionary<System.Transactions.IsolationLevel, string> {
                { System.Transactions.IsolationLevel.ReadCommitted, "SET TRANSACTION ISOLATION LEVEL READ COMMITTED" },
                { System.Transactions.IsolationLevel.ReadUncommitted, "SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED" },
                { System.Transactions.IsolationLevel.RepeatableRead, "SET TRANSACTION ISOLATION LEVEL REPEATABLE READ" },
                { System.Transactions.IsolationLevel.Serializable, "SET TRANSACTION ISOLATION LEVEL SERIALIZABLE" },
                { System.Transactions.IsolationLevel.Snapshot, "SET TRANSACTION ISOLATION LEVEL SNAPSHOT" }
            };
            TransactionManagerHelper.OverrideMaximumTimeout(TimeSpan.MaxValue);

            #region LogChangesDuringSave
            if (ConfigurationManager.AppSettings["LogChangesDuringSave"] != null) {
                LogChangesDuringSave = Convert.ToBoolean(ConfigurationManager.AppSettings["LogChangesDuringSave"]);
            }
            DatabaseLog.Info($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} LogChangesDuringSave: {LogChangesDuringSave}");
            #endregion

            #region SqlCommandTimeoutSecondsMinimum
            if (ConfigurationManager.AppSettings["SqlCommandTimeoutSecondsMinimum"] != null) {
                SqlCommandTimeoutSecondsMinimum = Convert.ToInt32(ConfigurationManager.AppSettings["SqlCommandTimeoutSecondsMinimum"]);
            }
            if (SqlCommandTimeoutSecondsMinimum < 30) SqlCommandTimeoutSecondsMinimum = 30;
            DatabaseLog.Info($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} SqlCommandTimeoutSecondsMinimum: {SqlCommandTimeoutSecondsMinimum}");
            #endregion

            #region SqlCommandTimeoutSeconds
            if (ConfigurationManager.AppSettings["SqlCommandTimeoutSeconds"] != null) {
                SqlCommandTimeoutSeconds = Convert.ToInt32(ConfigurationManager.AppSettings["SqlCommandTimeoutSeconds"]);
            }
            if (SqlCommandTimeoutSeconds < SqlCommandTimeoutSecondsMinimum) {
                SqlCommandTimeoutSeconds = SqlCommandTimeoutSecondsMinimum;
            }
            DatabaseLog.Info($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} SqlCommandTimeoutSeconds: {SqlCommandTimeoutSeconds}");
            #endregion

            #region TransactionScopeOptions
            if (ConfigurationManager.AppSettings["TransactionScopeOption"] != null) {
                var transactionScopeOption = TransactionScopeOption.Required;
                if (Enum.TryParse<TransactionScopeOption>(ConfigurationManager.AppSettings["TransactionScopeOption"], ignoreCase: true, result: out transactionScopeOption)) {
                    TransactionScopeOptions = transactionScopeOption;
                }
            }
            #endregion

            #region UpdateRetryLimit
            if (ConfigurationManager.AppSettings["UpdateRetryLimit"] != null) {
                UpdateRetryLimit = Convert.ToInt32(ConfigurationManager.AppSettings["UpdateRetryLimit"]);
            }
            if ((UpdateRetryLimit < 1) || (UpdateRetryLimit > 5)) {
                UpdateRetryLimit = 3;
            }
            DatabaseLog.Info($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} UpdateRetryLimit: {UpdateRetryLimit}");
            #endregion

            #region UpdateRetryInterval
            if (ConfigurationManager.AppSettings["UpdateRetryIntervalMinutes"] != null) {
                UpdateRetryInterval = TimeSpan.FromMinutes(Convert.ToInt32(ConfigurationManager.AppSettings["UpdateRetryIntervalMinutes"]));
            }
            if ((UpdateRetryInterval < TimeSpan.FromMinutes(1)) || (UpdateRetryInterval > TimeSpan.FromMinutes(15))) {
                UpdateRetryInterval = TimeSpan.FromMinutes(5);
            }
            DatabaseLog.Info($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} UpdateRetryInterval: {UpdateRetryInterval}");
            #endregion

        }
        public CountriesEntityManager()
            : this(null) {
        }

        /// <summary>
        /// Main constructor
        /// </summary>
        /// <param name="userIdentity">The identity of the principal specified for the context</param>
        /// <param name="proxyCreationEnabled">True to enable creation of change-tracking proxies</param>
        /// <param name="isolationLevel">The IsolationLevel to use, default = ReadCommitted</param>
        public CountriesEntityManager(IIdentity userIdentity,
            bool proxyCreationEnabled = true,
            System.Transactions.IsolationLevel isolationLevel = System.Transactions.IsolationLevel.ReadCommitted)
            : base(ConnectionString) {

            this.ProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
            this.ThreadId = Thread.CurrentThread.ManagedThreadId;
            this.UserIdentity = userIdentity ?? WindowsIdentity.GetCurrent();
            this.Configuration.ProxyCreationEnabled = proxyCreationEnabled;

            this.Database.Connection.ConnectionString = ConnectionString;
            if (SqlCommandTimeoutSeconds > 0) {
                this.Database.CommandTimeout = SqlCommandTimeoutSeconds;
            }
            if (!this.Database.CommandTimeout.HasValue) {
                this.Database.CommandTimeout = 1800;
            }

            // Default isolation level for TransactionScope is Serializable
            TransactionOptions = new TransactionOptions() {
                IsolationLevel = isolationLevel,
                Timeout = TimeSpan.FromSeconds(this.Database.CommandTimeout.Value)
            };

            BulkTransactionOptions = new TransactionOptions() {
                IsolationLevel = System.Transactions.IsolationLevel.Serializable,
                Timeout = TimeSpan.FromHours(2)
            };

            if (this.Database.Connection.State == ConnectionState.Closed) {
                this.Database.Connection.Open();
            }

            // set default IsolationLevel.  Normally with the implicit TransactionScope, it is Serializable
            // http://blogs.u2u.be/diederik/post/2010/06/29/Transactions-and-Connections-in-Entity-Framework-40.aspx
            this.Database.ExecuteSqlCommand(IsolationLevelCommands[isolationLevel]);

            this.ChangeTrackerDetails = new List<string>();
            this.validationSummary = new List<DbEntityValidationResult>();
        }
        #endregion

        #region Methods

        /// <summary>
        /// Adds an entity to the context after first checking if it already exists in the local context
        /// </summary>
        /// <typeparam name="TEntity">The entity Type</typeparam>
        /// <param name="entity">The entity</param>
        /// <remarks>
        /// Typically used for updated/deleted entities that may have been detached. 
        /// Should not be used if entity.DetachedState == DetachedState.Added and entity key is DatabaseGenerated (Identity)
        /// due to the default identity is usually zero.
        /// </remarks>
        public void AddEntity<TEntity>(TEntity entity) where TEntity : class, IObjectWithState {
            #region Validation
            if (entity == null) {
                throw new ArgumentNullException("entity");
            }
            #endregion

            bool addEntity = true;

            // don't look for entity if the key is 0 or Guid.Empty (exclude new DatabaseGenerated keys)
            if (entity.EntityKey != null) {
                var entityKeyText = entity.EntityKey.ToString();
                // assumes the keys in the tables are convertible to long or Guid
                if (!string.IsNullOrWhiteSpace(entityKeyText)) {
                    long.TryParse(entityKeyText, out long entityKeyLong);
                    Guid.TryParse(entityKeyText, out Guid entityKeyGuid);

                    if ((entityKeyLong != 0) || (entityKeyGuid != Guid.Empty)) {
                        var existingEntity = this.Set<TEntity>().Local
                            .Where(x => x.EntityKey == entity.EntityKey)
                            .FirstOrDefault();

                        if (existingEntity != null) {
                            addEntity = false;
                        }
                    }
                }
            }

            if (addEntity) {
                this.Set<TEntity>().Add(entity);
            }
        }

        /// <summary>
        /// Sets the change tracker entry EntityState to match the DetachedState
        /// </summary>
        /// <remarks>
        /// From "Programming Entity Framework: DbContext", Chapter 4: Working with Disconnected Entities Including N-Tier Applications:
        /// adding the root of a graph will cause every entity in the graph to be registered with the context as a new entity. 
        /// This behavior is the same if you use DbSet.Add or change the Stateproperty for an entity to Added. Once all the entities 
        /// are tracked by the state manager, you can then work your way around the graph, specifying the correct state for each entity. 
        /// It is possible to start by calling an operation that will register the root as an existing entity. This includes  
        /// DbSet.Attachor setting the Stateproperty to Unchanged,  Modified, or  Deleted. However, this approach isn’t recommended 
        /// because you run the risk of exceptions due to duplicate key values if you have added entities in your graph. If you register 
        /// the root as an existing entity, every entity in the graph will get registered as an existing entity. 
        /// 
        /// Because existing entities should all have unique primary keys, Entity Framework will ensure that you don’t register two 
        /// existing entities of the same type with the same key. If you have a new entity instance that will have its primary key value 
        /// generated by the database, you probably won’t bother assigning a value to the primary key; you’ll just leave it set to the
        /// default value. This means if your graph contains multiple new entities of the same type, they will have the same key value. 
        /// If you attach the root, Entity Framework will attempt to mark every entity as Unchanged, which will fail because you would 
        /// have two existing entities with the same key.
        /// 
        /// If you are Adding or Attaching a Root, then painting state throughout graph, It is recommended that you Add the root rather 
        /// than attaching it to avoid key conflicts for new entities.
        /// 
        /// After the root is Added, the ConvertState will perform the fixup.
        /// </remarks>
        private void AdjustChangeTrackerState() {

            var changeTrackerEntries = this.ChangeTracker.Entries().ToList();
            foreach (var changeTrackerEntry in changeTrackerEntries) {
                IObjectWithState stateInfo = changeTrackerEntry.Entity as IObjectWithState;
                if (stateInfo == null) {
                    throw new NotSupportedException("Entity objects must implement IObjectWithState.");
                }

                var convertedState = ConvertState(stateInfo.DetachedState);
                if (convertedState != changeTrackerEntry.State) {
                    // do not convert state if our custom state is Unchanged but the EntityState is Deleted.  
                    // EntityState may be deleted for relationship fixup during cascade delete.
                    if (changeTrackerEntry.State != EntityState.Deleted) {
                        // do not convert EntityState to our custom state if custom state is Unchanged but the EntityState is Modified.  
                        // EntityState may be modified for relationship fixup.
                        if (!((stateInfo.DetachedState == DetachedState.Unchanged) && (changeTrackerEntry.State == EntityState.Modified))) {
                            changeTrackerEntry.State = convertedState;
                        }
                    }
                    else {
                        // Do not set our custom DetachedState to match EntityState.Deleted.  It isn't necessary, and 
                        // During SaveChanges(), this will result in the following exception:
                        // System.Data.Entity.Infrastructure.DbUpdateConcurrencyException - InnerException: System.Data.OptimisticConcurrencyException
                        // Store update, insert, or delete statement affected an unexpected number of rows (0). 
                        // Entities may have been modified or deleted since entities were loaded. Refresh ObjectStateManager entries.
                        //
                        // stateInfo.DetachedState = State.Deleted;
                    }
                }

                // this needs to be performed even if the CHANGETRACKEREntry.State == EntityState.Deleted (converted previously)
                if (stateInfo.DetachedState == DetachedState.Unchanged) {
                    // stateInfo.StartingOriginalValues would only exist if the entity had materialized in a database query
                    // adding an entity then updating it may result in a null StartingOriginalValues
                    if (stateInfo.StartingOriginalValues != null) {
                        this.ApplyPropertyChanges(changeTrackerEntry.OriginalValues, stateInfo.StartingOriginalValues);
                    }
                    else {
                        // do not mark entire entity as modified.  That would result in all properties sent to the database during update.
                        // changeTrackerEntry.State = EntityState.Modified;
                    }
                }
            }
        }

        /// <summary>
        /// Restores the OriginalValues collection from the StartingOriginalValues created at the time of entity inception
        /// </summary>
        /// <remarks>Required for concurrency checks.  When SaveChanges is called, it compares the original/current values to determine 
        /// if an entity is modified and which properties to send in the update.
        /// </remarks>
        private void ApplyPropertyChanges(DbPropertyValues values, Dictionary<string, object> startingOriginalValues) {
            foreach (var entry in startingOriginalValues) {
                var childStartingOriginalValues = entry.Value as Dictionary<string, object>;
                if (childStartingOriginalValues != null) {
                    this.ApplyPropertyChanges((DbPropertyValues)values[entry.Key], childStartingOriginalValues);
                }
                else {
                    values[entry.Key] = entry.Value;
                }
            }
        }

        /// <summary>
        /// Attaches an entity to the ObjectContext
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="entity">The entity object</param>
        /// <param name="entityState">The EntityState to set</param>
        /// <returns>DbEntityEntry of type TEntity</returns>
        /// <remarks>
        /// Useful for attaching "stub" entities, typically created for the purpose of deleting an entity with only the entity key,
        /// or attaching entities that were queried with .AsNoTracking()
        /// </remarks>
        public DbEntityEntry<TEntity> Attach<TEntity>(TEntity entity) where TEntity : class, IObjectWithState {
            // Check if the same entity with same key values are already there
            DbEntityEntry<TEntity> changeTrackerEntry = this.Entry<TEntity>(entity);

            if ((changeTrackerEntry == null) || (changeTrackerEntry.State == EntityState.Detached)) {
                // throws InvalidOperationException if entity had been active in the current context but not detached
                this.Set<TEntity>().Attach(entity);
                changeTrackerEntry = this.Entry<TEntity>(entity);

                if (changeTrackerEntry != null) {
                    if (entity.DetachedState == DetachedState.Added) {
                        changeTrackerEntry.State = EntityState.Added;
                    }
                    else if (entity.DetachedState == DetachedState.Deleted) {
                        changeTrackerEntry.State = EntityState.Deleted;
                    }
                    // DetachedState.Modified is intended for use only when attaching an entity that was created with .NoTracking().
                    else if (entity.DetachedState == DetachedState.Modified) {
                        changeTrackerEntry.State = EntityState.Modified;
                    }
                }
            }

            return changeTrackerEntry;
        }

        /// <summary>
        /// Converts the entity change tracker state to the state reported from the client.
        /// Modified entities are marked as Unchanged.
        /// </summary>
        private EntityState ConvertState(DetachedState state) {
            switch (state) {
                case DetachedState.Added:
                    return EntityState.Added;
                case DetachedState.Deleted:
                    return EntityState.Deleted;
                case DetachedState.Modified:
                    return EntityState.Modified;
                default:
                    return EntityState.Unchanged;
            }
        }

        /// <summary>
        /// Detaching child entities results in the collections cleared and attached 1:1 entities nulled.
        /// </summary>
        /// <param name="entity"></param>
        public void Detach(IObjectWithState entity) {
            this.Detach(entity, true);
        }
        public void Detach(IObjectWithState entity, bool detachChildren) {
            DbEntityEntry changeTrackerEntry = this.Entry(entity);
            if (changeTrackerEntry != null) {
                if (detachChildren) {
                    var entityType = changeTrackerEntry.Entity.GetType();
                    foreach (var item in entityType.GetProperties()) {
                        // a timestamp byte[] may be considered an IList or ICollection
                        if (item.GetValue(changeTrackerEntry.Entity) as byte[] != null) continue;
                        var navigationPropertyCollection = item.GetValue(changeTrackerEntry.Entity) as System.Collections.IList;
                        if (navigationPropertyCollection != null) {
                            for (int index = 0; index < navigationPropertyCollection.Count; index++) {
                                this.Detach(navigationPropertyCollection[index] as IObjectWithState, detachChildren);
                            }
                        }
                    }
                }

                if (changeTrackerEntry.State != EntityState.Detached) {
                    this.ObjectContext.Detach(changeTrackerEntry.Entity);
                }
            }
        }

        private void GetChangeTrackerEntriesDetail() {
            this.ChangeTrackerDetails.Clear();
            var changeTrackerEntries = from dbEntityEntry in this.ChangeTracker.Entries()
                                       where dbEntityEntry.State != EntityState.Unchanged
                                       select dbEntityEntry;

            foreach (var entry in changeTrackerEntries) {

                this.ChangeTrackerDetails.Add($"{entry.State} entity type: {entry.Entity.GetType()}:");

                switch (entry.State) {
                    case EntityState.Added:
                        this.LogPropertyValues(entry.CurrentValues, entry.CurrentValues.PropertyNames);
                        break;

                    case EntityState.Deleted:
                        this.LogPropertyValues(entry.OriginalValues, this.GetKeyPropertyNames(entry.Entity));
                        break;

                    case EntityState.Modified:
                        var modifiedPropertyNames =
                            from propertyName in entry.CurrentValues.PropertyNames
                            where entry.Property(propertyName).IsModified
                            select propertyName;

                        this.LogPropertyValues(entry.CurrentValues, this.GetKeyPropertyNames(entry.Entity).Concat(modifiedPropertyNames));
                        break;
                }
            }
        }

        /// <summary>
        /// Gets the key property names for the specified entity
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <returns>A list of key property names</returns>
        private IEnumerable<string> GetKeyPropertyNames(object entity) {
            return this.ObjectContext
                .ObjectStateManager
                .GetObjectStateEntry(entity)
                .EntityKey
                .EntityKeyValues
                .Select(k => k.Key);
        }

        /// <summary>
        /// Gets the validation result for the specified entity
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="entity">The entity object</param>
        /// <returns>The DbEntityValidationResult</returns>
        public DbEntityValidationResult GetValidationResult<TEntity>(TEntity entity) where TEntity : class, IObjectWithState {
            DbEntityValidationResult result = null;
            var entry = this.Attach<TEntity>(entity);
            if (entry != null) result = entry.GetValidationResult();
            return result;
        }

        /// <summary>
        /// Determines if an entity object is a change-tracked proxy
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <returns>True if change tracked proxy</returns>
        private static bool IsChangeTrackedProxy(object entity) {

            bool isChangeTrackedProxy = false;

            if (entity != null) {
                var objectContextType = ObjectContext.GetObjectType(entity.GetType());
                var objectType = entity.GetType();
                isChangeTrackedProxy = objectContextType != objectType;
            }

            return isChangeTrackedProxy;
        }

        /// <summary>
        /// Logs a summary of database operations
        /// </summary>
        private void LogChangeTrackerEntriesSummary() {
            int addedEntities = 0;
            int deletedEntities = 0;
            int modifiedEntities = 0;
            int totalEntitiesToSave = 0;

            if (this.ChangeTracker.Entries().Count() > 0) {
                addedEntities = this.ChangeTracker.Entries().Where(x => (x != null) && (x.State == EntityState.Added)).Count();
                deletedEntities = this.ChangeTracker.Entries().Where(x => (x != null) && (x.State == EntityState.Deleted)).Count();
                modifiedEntities = this.ChangeTracker.Entries().Where(x => (x != null) && (x.State == EntityState.Modified)).Count();
                totalEntitiesToSave = addedEntities + deletedEntities + modifiedEntities;
            }

            DatabaseLog.Debug($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} Total number of entities to save: {totalEntitiesToSave}; Added: {addedEntities}; Deleted: {deletedEntities}; Modified: {modifiedEntities}");
        }

        /// <summary>
        /// Logs the values of an entity's properties
        /// </summary>
        /// <param name="values">The list of DbPropertyValues</param>
        /// <param name="propertiesToPrint">The list of properties to log</param>
        /// <param name="indent">The indent level of the log</param>
        private void LogPropertyValues(DbPropertyValues values, IEnumerable<string> propertiesToPrint, int indent = 1) {
            foreach (var propertyName in propertiesToPrint) {
                var complexPropertyValues = values[propertyName] as DbPropertyValues;
                if (complexPropertyValues != null) {
                    this.ChangeTrackerDetails.Add($"{string.Empty.PadLeft(indent)}- Property name: {propertyName}:");
                    this.LogPropertyValues(complexPropertyValues, complexPropertyValues.PropertyNames, indent++);
                }
                else {
                    this.ChangeTrackerDetails.Add($"{string.Empty.PadLeft(indent)}- {propertyName}: {values[propertyName]}");
                }
            }
        }

        /// <summary>
        /// Logs the contents of a list of validation errors
        /// </summary>
        /// <param name="results"></param>
        private void LogValidationErrors(IEnumerable<DbEntityValidationResult> results) {
            this.validationSummary = new List<DbEntityValidationResult>();
            int counter = 0;
            foreach (DbEntityValidationResult result in results) {
                this.validationSummary.Add(result);

                counter++;

                DatabaseLog.Debug($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} Validation error entry#: {counter}; Entity Type: {result.Entry.Entity.GetType().Name}; error count: {result.ValidationErrors.Count}");

                foreach (DbValidationError error in result.ValidationErrors) {
                    var message = $"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()}  - Property Name: {error.PropertyName}; Error Message: {error.ErrorMessage}";
                    Console.WriteLine(message);
                    DatabaseLog.Debug(message);
                }
            }
        }

        /// <summary>
        /// Removes an entity after first checking if it already exists in the local DbContext
        /// </summary>
        /// <typeparam name="TEntity">The entity Type</typeparam>
        /// <param name="entity">The entity</param>
        /// <returns>True if successfully attached and marked for deletion</returns>
        /// <remarks>Cannot Add then Remove the entity.  It either needs to be in the Local DbContext or a change-tracked proxy that can be attached.
        /// </remarks>
        public bool RemoveEntity<TEntity>(TEntity entity) where TEntity : class, IObjectWithState {

            bool success = false;

            entity.DetachedState = DetachedState.Deleted;

            var existingEntity = this.Set<TEntity>().Local
                .Where(x => x.EntityKey == entity.EntityKey)
                .FirstOrDefault();

            if (existingEntity == null) {
                if (IsChangeTrackedProxy(entity)) {
                    this.Attach<TEntity>(entity);
                }
            }

            existingEntity = this.Set<TEntity>().Local
                .Where(x => x.EntityKey == entity.EntityKey)
                .FirstOrDefault();

            if (existingEntity != null) {
                this.Set<TEntity>().Remove(entity);
                success = true;
            }
            else {
                // Add entity with entity.DetachedState = DetachedState.Deleted;
                this.AddEntity<TEntity>(entity);
                success = true;
            }

            return success;
        }

        /// <summary>
        /// Resets the DetachedState to Unchanged and rebuilds StartingOriginalValues
        /// </summary>
        private void ResetStateAndOriginalValues() {
            foreach (var changeTrackerEntry in this.ChangeTracker.Entries<IObjectWithState>()) {
                IObjectWithState stateInfo = changeTrackerEntry.Entity as IObjectWithState;

                if ((stateInfo != null) && (stateInfo.DetachedState != DetachedState.Deleted)) {
                    if (changeTrackerEntry.OriginalValues != null) {
                        stateInfo.StartingOriginalValues = base.BuildOriginalValues(changeTrackerEntry.OriginalValues);
                    }

                    stateInfo.DetachedState = DetachedState.Unchanged;
                }

                changeTrackerEntry.State = EntityState.Unchanged;
            }
        }

        /// <summary>
        /// Saves changes to objects in the graph
        /// </summary>
        /// <returns>The number of entities added/updated/deleted</returns>
        /// <remarks>
        /// The entity should be added/attached/deleted to/from the context before saving changes.
        /// If we have a large collection of new entities and we are confident that the default change detection is not required,
        /// disabling  the change tracking options: SaveOptions.DetectChangesBeforeSave | SaveOptions.AcceptAllChangesAfterSave
        /// will yield significant performance improvement.
        /// </remarks>
        public override int SaveChanges() {
            return this.SaveChanges(false);
        }
        public int SaveChanges(bool bulkUpdate) {
            int changesSaved = 0;

#if DEBUG
            this.ValidateProcessAndThreadId();
#endif

            this.AdjustChangeTrackerState();

            // DetectChanges also performs relationship fix-up for any relationships that it detects have changed. 
            // It ensures that the navigation properties and foreign key properties are synchronized.
            this.ChangeTracker.DetectChanges();

            int retryCount = 0;
            while (retryCount++ < UpdateRetryLimit) {
                try {
                    if (bulkUpdate) {
                        // require a new Transaction to ensure we can force the bulk TransactionOption, which includes IsolationLevel.Serializable
                        // to ensure integrity for large updates with complex object graphs.
                        // As an alternative to this, use BulkCopyDataTable() for single table bulk updates
                        using (var transactionScope = new TransactionScope(TransactionScopeOption.RequiresNew, this.BulkTransactionOptions)) {
                            changesSaved = this.ObjectContext.SaveChanges(SaveOptions.None);
                            transactionScope.Complete();

                            // If the transaction does not fail, accept the changes.  If it fails, it may still be possible to recover
                            // as the changes have not yet been discarded.
                            // http://blogs.msdn.com/b/alexj/archive/2009/01/11/savechanges-false.aspx
                            this.ObjectContext.AcceptAllChanges();
                        }
                    }
                    else {
                        using (var transactionScope = new TransactionScope(TransactionScopeOptions, this.TransactionOptions)) {

                            this.LogValidationErrors(GetValidationErrors());
                            this.LogChangeTrackerEntriesSummary();

                            if (LogChangesDuringSave) {
                                if (this.ChangeTrackerDetails.Count == 0) {
                                    this.GetChangeTrackerEntriesDetail();
                                }

                                if (this.ChangeTrackerDetails.Count > 0) {
                                    DatabaseLog.Debug($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} change details:");
                                    DatabaseLog.Debug(this.ChangeTrackerDetails.ToFormattedString());
                                }
                            }

                            this.ChangeTrackerDetails.Clear();

                            try {
                                changesSaved = base.SaveChanges();
                                transactionScope.Complete();
                            }
                            catch (DbEntityValidationException ex) {

                                var validationErrors = new List<string> {
                                    $"{ex.Message} The validation errors are: "
                                };
                                foreach (DbEntityValidationResult validationResult in ex.EntityValidationErrors) {
                                    string entityName = validationResult.Entry.Entity.GetType().Name;
                                    foreach (DbValidationError error in validationResult.ValidationErrors) {
                                        validationErrors.Add($"{entityName}.{error.PropertyName}: {error.ErrorMessage}");
                                    }
                                }

                                throw new DbEntityValidationException(validationErrors.ToFormattedString(), ex.EntityValidationErrors);
                            }
                        } // using (var transactionScope = new TransactionScope(TransactionScopeOption.Required, this.TransactionOptions)) {
                    }

                    break;
                }
                catch (SqlException e) {
                    if (retryCount > UpdateRetryLimit) throw;
                    if (!e.Message.ToLowerInvariant().Contains("timeout") && !e.Message.ToLowerInvariant().Contains("deadlock")) throw;

                    DatabaseLog.Warn($"[ThreadID: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} Exception: {e.VerboseExceptionString()}");
                    DatabaseLog.Warn($"[ThreadID: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} Timeout occurred inserting records into database.  Attempting retry: {retryCount} of: {UpdateRetryLimit} in: {UpdateRetryInterval} minutes.");
                    Thread.Sleep(UpdateRetryInterval);
                }
            } // while (retryCount++ < UpdateRetryLimit) {

            this.ResetStateAndOriginalValues();

            return changesSaved;
        }

        /// <summary>
        /// Validate that this instance is not used by other processes/threads
        /// </summary>
        private void ValidateProcessAndThreadId() {
            if ((this.ProcessId > 0) && (this.ThreadId > 0)) {
                var currentProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
                var currentThreadId = Thread.CurrentThread.ManagedThreadId;

                if ((currentProcessId != this.ProcessId) || (currentThreadId != this.ThreadId)) {
                    throw new NotSupportedException("Attempt to use Entity context from another process/thread from the creating process/thread.");
                }
            }
        }

        #region Native database methods
        /// <summary>
        /// Uses SqlBulkCopy to load a data table
        /// </summary>
        /// <param name="dataTable">The DataTable</param>
        /// <param name="sqlBulkCopyOptions">The SqlBulkCopyOptions.</param>
        /// <param name="batchSize">The number of rows to bulk copy per batch.</param>
        /// <param name="sqlTransaction">The transaction to use</param>
        public void BulkCopyDataTable(
            DataTable dataTable,
            SqlBulkCopyOptions sqlBulkCopyOptions = SqlBulkCopyOptions.TableLock,
            SqlTransaction sqlTransaction = null,
            int batchSize = 0) {

            if (dataTable == null) {
                throw new ArgumentNullException("dataTable");
            }

            var stopwatch = Stopwatch.StartNew();

            DatabaseLog.Debug($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} table row count: {dataTable.Rows.Count}");

            int retryCount = 0;
            while (retryCount++ < UpdateRetryLimit) {
                try {
                    using (var sqlConnection = new SqlConnection(ConnectionString)) {
                        sqlConnection.Open();
                        using (var sqlBulkCopy = new SqlBulkCopy(sqlConnection, sqlBulkCopyOptions, sqlTransaction)) {
                            sqlBulkCopy.BulkCopyTimeout = (int)this.BulkTransactionOptions.Timeout.TotalSeconds;
                            sqlBulkCopy.BatchSize = batchSize;
                            sqlBulkCopy.NotifyAfter = 50000;
                            sqlBulkCopy.SqlRowsCopied += new SqlRowsCopiedEventHandler(OnSqlBulkCopyRowsCopied);

                            for (int index = 0; index < dataTable.Columns.Count; index++) {
                                sqlBulkCopy.ColumnMappings.Add(dataTable.Columns[index].ColumnName, dataTable.Columns[index].ColumnName);
                            }

                            sqlBulkCopy.DestinationTableName = dataTable.TableName;

                            DatabaseLog.Debug($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} writing: {dataTable.Rows.Count} rows to table: {dataTable.TableName}");

                            sqlBulkCopy.WriteToServer(dataTable);
                            dataTable.Rows.Clear();

                            #region for analyzing data mismatch issues
                            //using (ValidatingDataReader validatingDataReader = new ValidatingDataReader(csv, sqlConnection, sqlBulkCopy))
                            //{
                            //    sqlBulkCopy.WriteToServer(validatingDataReader);
                            //} 
                            #endregion

                        }
                    }

                    break;
                }
                catch (SqlException e) {
                    if (retryCount > UpdateRetryLimit) throw;
                    if (!e.Message.ToLowerInvariant().Contains("timeout") && !e.Message.ToLowerInvariant().Contains("deadlock")) throw;

                    DatabaseLog.Warn($"[ThreadID: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} Exception: {e.VerboseExceptionString()}");
                    DatabaseLog.Warn($"[ThreadID: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} Timeout occurred inserting records into database.  Attempting retry: {retryCount} of: {UpdateRetryLimit} in: {UpdateRetryInterval} minutes.");
                    Thread.Sleep(UpdateRetryInterval);
                }
            } // while (retryCount++ < UpdateRetryLimit) {

            DatabaseLog.Debug($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} Finished.  Time required: {stopwatch.Elapsed}");

        }

        /// <summary>
        /// Deletes the list of rows from the specified table in bulk.
        /// </summary>
        /// <param name="tableName">The table name</param>
        /// <param name="rowIds">The list of row id</param>
        /// <param name="idName">The name of the Id column.</param>/// 
        /// <param name="batchDeleteCount">The number of rows to delete during a single command. Minimum 1, Maximum 10000</param>
        /// <returns>The total number of rows deleted.</returns>
        /// <remarks>Can be must faster For large number of rows to delete.</remarks>
        public int BulkDeleteRows(string tableName, List<string> rowIds, string idName = "Id", int batchDeleteCount = 1000) {
            DatabaseLog.Debug($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} table: {tableName ?? "NULL"} idName: {idName ?? "NULL"} rows to delete: {(rowIds == null ? "NULL" : rowIds.Count.ToString())} batchDeleteCount: {batchDeleteCount}");

            #region Validation
            if (string.IsNullOrWhiteSpace(tableName)) {
                throw new ArgumentNullException("tableName");
            }
            if (rowIds == null) {
                throw new ArgumentNullException("rowIds");
            }
            if (batchDeleteCount < 1) {
                throw new ArgumentOutOfRangeException("batchDeleteCount");
            }
            #endregion

            int skipCount = 0;
            int takeCount = (batchDeleteCount > 10000) ? 10000 : batchDeleteCount;
            int rowsRemoved = 0;
            int rowsRemovedTotal = 0;

            if (rowIds.Count > 0) {
                while (skipCount < rowIds.Count) {

                    var interimList = rowIds
                        .Skip(skipCount)
                        .Take(takeCount)
                        .ToList();

                    skipCount += interimList.Count;
                    var rowIdsArray = new StringBuilder();
                    rowIdsArray.Append("(");

                    for (int index = 0; index < (interimList.Count - 1); index++) {
                        rowIdsArray.Append($"'{interimList[index]}',");
                    }
                    // append last id without the trailing comma
                    rowIdsArray.Append($"'{interimList.Last()}'");
                    rowIdsArray.Append(")");

                    DatabaseLog.Debug($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} table: {tableName} rows to delete: {interimList.Count} skip count: {skipCount} total count to delete: {rowIds.Count}");

                    var sqlCommand = new SqlCommand();
                    sqlCommand.CommandText = $"DELETE FROM {tableName} WHERE [Id] IN {rowIdsArray.ToString()}";
                    sqlCommand.CommandTimeout = 120 + (rowIdsArray.Length * 2);

                    rowsRemoved = this.ExecuteNonQuerySql(sqlCommand);
                    rowsRemovedTotal += rowsRemoved;

                    DatabaseLog.Debug($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} table: {tableName} rows deleted: {rowsRemoved}");

                } // while (skipCount < rowIds.Count) {
            } // if (rowIds.Count > 0) {

            DatabaseLog.Debug($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} Finished deleting rows from table: {tableName}. Total rows deleted: {rowsRemovedTotal}");

            return rowsRemovedTotal;

        }

        /// <summary>
        /// Copies a collection of entities to a DataTable, and calls BulkCopyDataTable
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entities"></param>
        /// <remarks>Bulk insert without specifying SqlBulkCopyOptions.CheckConstraints will cause foreign keys to become untrusted.</remarks>
        public void BulkInsertRows<TEntity>(
            IEnumerable<TEntity> entities,
            SqlBulkCopyOptions sqlBulkCopyOptions = SqlBulkCopyOptions.TableLock)
            where TEntity : class {
            if (entities == null) {
                throw new ArgumentNullException("entities");
            }

            DatabaseLog.Debug($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} entity count: {entities.Count()}");

            var dataTable = this.GetDataTable(entities);
            this.BulkCopyDataTable(dataTable: dataTable, sqlBulkCopyOptions: sqlBulkCopyOptions);
        }

        /// <summary>
        /// Gets a data for a collection of entities
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="entities">The collection of entities</param>
        /// <returns>The Datatable of entities</returns>
        public DataTable GetDataTable<TEntity>(IEnumerable<TEntity> entities) where TEntity : class {

            if (entities == null) {
                throw new ArgumentNullException("entities");
            }

            return entities.CopyToDataTable();
        }

        /// <summary>
        /// Provides mechanism to run native T-SQL against database
        /// </summary>
        /// <returns>number of rows affected or -2 if error</returns>
        /// <remarks>Example use is to truncate a table or delete large numbers of rows, bulk import of data.</remarks>
        public int ExecuteNonQuerySql(SqlCommand sqlCommand) {
            sqlCommand.Connection = this.Database.Connection as SqlConnection;

            int returnValue = 0;
            int retryCount = 0;
            while (retryCount++ < UpdateRetryLimit) {
                try {
                    if (sqlCommand.Connection.State != ConnectionState.Open) {
                        sqlCommand.Connection.Open();
                    }

                    using (var transactionScope = new TransactionScope(TransactionScopeOptions, this.TransactionOptions)) {
                        returnValue = sqlCommand.ExecuteNonQuery();
                        transactionScope.Complete();
                    }
                    break;
                }
                catch (SqlException e) {
                    if (retryCount > UpdateRetryLimit) throw;
                    if (!e.Message.ToLowerInvariant().Contains("timeout") && !e.Message.ToLowerInvariant().Contains("deadlock")) throw;

                    DatabaseLog.Warn($"[ThreadID: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} Exception: {e.VerboseExceptionString()}");
                    DatabaseLog.Warn($"[ThreadID: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} Timeout occurred inserting records into database.  Attempting retry: {retryCount} of: {UpdateRetryLimit} in: {UpdateRetryInterval} minutes.");
                    Thread.Sleep(UpdateRetryInterval);
                }
                catch (Exception e) {
                    DatabaseLog.Error($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} SQL command: {sqlCommand.CommandText} TimeoutSeconds: {sqlCommand.CommandTimeout} Error: {e.VerboseExceptionString()}");
                    this.LastError = e;
                    return -2;
                }
            }
            return returnValue;
        }

        /// <summary>
        /// Provides mechanism to run native T-SQL against database
        /// </summary>
        /// <param name="sql"></param>
        /// <remarks>Example use is to truncate a table or delete large numbers of rows, bulk import of data.</remarks>
        /// <returns>A single object for the scalar value to return</returns>
        public object ExecuteScalarSql(SqlCommand sqlCommand) {
            sqlCommand.Connection = this.Database.Connection as System.Data.SqlClient.SqlConnection;
            object returnValue = null;

            int retryCount = 0;
            while (retryCount++ < UpdateRetryLimit) {
                try {
                    if (sqlCommand.Connection.State != ConnectionState.Open) {
                        sqlCommand.Connection.Open();

                        if (sqlCommand.CommandTimeout < SqlCommandTimeoutSecondsMinimum) {
                            sqlCommand.CommandTimeout = SqlCommandTimeoutSecondsMinimum;
                        }
                    }

                    using (var transactionScope = new TransactionScope(TransactionScopeOptions, this.TransactionOptions)) {
                        returnValue = sqlCommand.ExecuteScalar();
                        transactionScope.Complete();
                    }

                    break;
                }
                catch (SqlException e) {
                    if (retryCount > UpdateRetryLimit) throw;
                    if (!e.Message.ToLowerInvariant().Contains("timeout") && !e.Message.ToLowerInvariant().Contains("deadlock")) throw;

                    DatabaseLog.Warn($"[ThreadID: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} Exception: {e.VerboseExceptionString()}");
                    DatabaseLog.Warn($"[ThreadID: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} Timeout occurred inserting records into database.  Attempting retry: {retryCount} of: {UpdateRetryLimit} in: {UpdateRetryInterval} minutes.");
                    Thread.Sleep(UpdateRetryInterval);
                }
                catch (Exception e) {
                    DatabaseLog.Error($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} Error executing scalar SQL command: {e.VerboseExceptionString()}");
                    this.LastError = e;
                }
            }

            if ((returnValue == null) || (returnValue.GetType() == typeof(System.DBNull))) return null;
            return returnValue;
        }

        /// <summary>
        /// Provides mechanism to run native T-SQL against database and return a table
        /// For use when not returning a typed entity set.
        /// </summary>
        public DataTable ExecuteSql(SqlCommand sqlCommand) {
            sqlCommand.Connection = this.Database.Connection as SqlConnection;
            DataTable dataTable = new DataTable();

            int retryCount = 0;
            while (retryCount++ < UpdateRetryLimit) {
                try {

                    if (sqlCommand.Connection.State != ConnectionState.Open) {
                        sqlCommand.Connection.Open();
                    }

                    using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader()) {
                        dataTable.Load(sqlDataReader);
                    }

                }
                catch (SqlException e) {
                    if (retryCount > UpdateRetryLimit) throw;
                    if (!e.Message.ToLowerInvariant().Contains("timeout") && !e.Message.ToLowerInvariant().Contains("deadlock")) throw;

                    DatabaseLog.Warn($"[ThreadID: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} Exception: {e.VerboseExceptionString()}");
                    DatabaseLog.Warn($"[ThreadID: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} Timeout occurred inserting records into database.  Attempting retry: {retryCount} of: {UpdateRetryLimit} in: {UpdateRetryInterval} minutes.");
                    Thread.Sleep(UpdateRetryInterval);
                }
                catch (Exception e) {
                    DatabaseLog.Error($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} Error executing SQL command: {e.VerboseExceptionString()}");
                    this.LastError = e;
                }

                break;
            }


            return dataTable;
        }

        /// <summary>
        /// Executes a query directly against the data source and returns a sequence
        /// of typed results. Specify the entity set and the merge option so that query
        /// results can be tracked as entities.
        /// </summary>
        public ObjectResult<TEntity> ExecuteSql<TEntity>(string sql, params object[] parameters) where TEntity : class {
            return this.ObjectContext.ExecuteStoreQuery<TEntity>(sql, parameters);
        }
        public ObjectResult<TEntity> ExecuteSql<TEntity>(string sql, string entitySetName, MergeOption mergeOption, params object[] parameters) where TEntity : class {
            return this.ObjectContext.ExecuteStoreQuery<TEntity>(sql, entitySetName, mergeOption, parameters);
        }
        /// <summary>
        /// Executes the given stored procedure or function that is defined in the data
        /// source and expressed in the conceptual model, with the specified parameters
        /// </summary>
        public ObjectResult<TEntity> ExecuteStoredProcedure<TEntity>(string functionName, params ObjectParameter[] parameters) where TEntity : class {
            return this.ExecuteStoredProcedure<TEntity>(functionName, parameters);
        }
        public ObjectResult<TEntity> ExecuteStoredProcedure<TEntity>(string functionName, MergeOption mergeOption, params ObjectParameter[] parameters) where TEntity : class {
            return this.ObjectContext.ExecuteFunction<TEntity>(functionName, mergeOption, parameters);
        }

        /// <summary>
        /// Gets the last identity used for a table that uses identity keys
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public long GetLastIdentityKey(string tableName) {
            DatabaseLog.Debug($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} tableName: {tableName ?? "NULL"}");
            if (string.IsNullOrWhiteSpace(tableName)) return -1;

            long lastIdentityKey = -1;
            int timeoutSeconds = 120;
            var stopwatch = Stopwatch.StartNew();

            try {

                using (var sqlCommand = new SqlCommand()) {
                    sqlCommand.CommandText = $"SELECT IDENT_CURRENT('{tableName}')";
                    sqlCommand.CommandTimeout = timeoutSeconds;

                    DatabaseLog.Debug($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} Executing command: {sqlCommand.CommandText}");

                    object returnValue = this.ExecuteScalarSql(sqlCommand);
                    if (returnValue != null) {
                        try {
                            lastIdentityKey = Convert.ToInt64(returnValue);
                        }
                        catch { }
                    }
                }

                DatabaseLog.Debug($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} finished.  Return value: {lastIdentityKey}");
            }
            catch (Exception e) {
                DatabaseLog.Error($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} {e.VerboseExceptionString()}");
                this.LastError = e;
                return -1;
            }
            finally {
                DatabaseLog.Debug($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} finished.  Time required: {stopwatch.Elapsed}");
            }

            return lastIdentityKey;
        }

        /// <summary>
        /// Gets the table column name for the specified entity type/property name
        /// </summary>
        /// <param name="entityType">The entity taype</param>
        /// <param name="propertyName">The property name</param>
        /// <returns>The Table.ColumnName</returns>
        /// <remarks>https://romiller.com/2015/08/05/ef6-1-get-mapping-between-properties-and-columns/</remarks>
        public string GetColumnName(Type type, string propertyName) {
            var metadata = this.ObjectContext.MetadataWorkspace;

            // Get the part of the model that contains info about the actual CLR types
            var objectItemCollection = ((ObjectItemCollection)metadata.GetItemCollection(DataSpace.OSpace));

            // Get the entity type from the model that maps to the CLR type
            var entityType = metadata
                .GetItems<EntityType>(DataSpace.OSpace)
                .Single(e => objectItemCollection.GetClrType(e) == type);

            // Get the entity set that uses this entity type
            var entitySet = metadata
                .GetItems<EntityContainer>(DataSpace.CSpace)
                .Single()
                .EntitySets
                .Single(s => s.ElementType.Name == entityType.Name);

            // Find the mapping between conceptual and storage model for this entity set
            var mapping = metadata.GetItems<EntityContainerMapping>(DataSpace.CSSpace)
                .Single()
                .EntitySetMappings
                .Single(s => s.EntitySet == entitySet);

            // Find the storage entity set (table) that the entity is mapped
            var tableEntitySet = mapping
                .EntityTypeMappings.Single()
                .Fragments.Single()
                .StoreEntitySet;

            // Return the table name from the storage entity set
            var tableName = tableEntitySet.MetadataProperties["Table"].Value ?? tableEntitySet.Name;

            // Find the storage property (column) that the property is mapped
            var columnName = mapping
                .EntityTypeMappings.Single()
                .Fragments.Single()
                .PropertyMappings
                .OfType<ScalarPropertyMapping>()
                      .Single(m => m.Property.Name == propertyName)
                .Column
                .Name;

            return tableName + "." + columnName;
        }

        private void OnSqlBulkCopyRowsCopied(object sender, SqlRowsCopiedEventArgs e) {
            DatabaseLog.Debug($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} SqlBulkCopy current row count: {e.RowsCopied}");
        }
        #endregion

        #endregion
    }
}
