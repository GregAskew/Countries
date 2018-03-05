namespace Countries.DomainModel {

    #region Usings
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;
    using System.Data.Entity.Core.Objects;
    using System.Collections.Generic;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Validation;
    using System.Configuration;
    using System.Threading;
    using Extensions;
    using System.Diagnostics;
    #endregion

    /// <summary>
    /// Wrapper class for DbContext. 
    /// Contains DbSet members, and performs internal functions for creating model and entity objects.
    /// </summary>
    public abstract class CountriesEntities : DbContext {

        #region Members

        // The calling assembly must have initialized log4net for this to work
        protected internal static readonly log4net.ILog DatabaseLog = log4net.LogManager.GetLogger("databaseLogger");

        /// <summary>
        /// Returns this as IObjectContextAdapter.ObjectContext, for the purpose of accessing the traditional ObjectContext
        /// </summary>
        public ObjectContext ObjectContext {
            get {
                var objectContextAdapter = this as IObjectContextAdapter;
                return objectContextAdapter.ObjectContext;
            }
        }

        #region DBContext.Configuration members
        private static bool AutoDetectChangesEnabled { get; set; }
        private static bool LazyLoadingEnabled { get; set; }
        private static bool ProxyCreationEnabled { get; set; }
        private static bool ValidateOnSaveEnabled { get; set; }
        #endregion

        #region DbSet Members
        public DbSet<CallingCode> CallingCodes { get; set; }
        public DbSet<Continent> Continents { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<Currency> Currencies { get; set; }

        public DbQuery<CountryCurrencyInfo> CountryCurrencyInfos {
            get { return this.Set<CountryCurrencyInfo>().AsNoTracking(); }
        }
        public DbQuery<CountryInfo> CountryInfos {
            get { return this.Set<CountryInfo>().AsNoTracking(); }
        }
        public DbQuery<CountryTimeZoneInfo> CountryTimeZoneInfos {
            get { return this.Set<CountryTimeZoneInfo>().AsNoTracking(); }
        }
        public DbSet<TimeZone> TimeZones { get; set; }
        #endregion
        #endregion

        #region Constructor
        /// <summary>
        /// The constructors for the DbContext wrapper
        /// </summary>
        /// <remarks>
        /// Lazy Loading disabled:
        /// http://msdn.microsoft.com/en-us/library/vstudio/bb896304%28v=vs.100%29.aspx
        /// With binary serialization and data contract serialization, related objects are serialized together with the primary object. 
        /// XML serialization does not serialize related objects. When serializing entities, disable lazy loading. Lazy loading performs a query 
        /// for each relationship navigation property that is accessed, and both binary and WCF data contract serializers access all 
        /// relationship navigation properties. This can cause many unexpected queries to be performed during serialization. For more information, 
        /// see Serializing Objects: http://msdn.microsoft.com/en-us/library/vstudio/bb738446(v=vs.100).aspx
        /// </remarks>
        static CountriesEntities() {
            AutoDetectChangesEnabled = ConfigurationManager.AppSettings["AutoDetectChangesEnabled"] != null
                ? Convert.ToBoolean(ConfigurationManager.AppSettings["AutoDetectChangesEnabled"])
                : false;
            LazyLoadingEnabled = ConfigurationManager.AppSettings["LazyLoadingEnabled"] != null
                ? Convert.ToBoolean(ConfigurationManager.AppSettings["LazyLoadingEnabled"])
                : false;
            ProxyCreationEnabled = ConfigurationManager.AppSettings["ProxyCreationEnabled"] != null
                ? Convert.ToBoolean(ConfigurationManager.AppSettings["ProxyCreationEnabled"])
                : true;
            ValidateOnSaveEnabled = ConfigurationManager.AppSettings["ValidateOnSaveEnabled"] != null
                ? Convert.ToBoolean(ConfigurationManager.AppSettings["ValidateOnSaveEnabled"])
                : true;
        }

        public CountriesEntities()
            : this("name=CountriesEntities") {
        }

        public CountriesEntities(string nameOrConnectionString)
            : base(nameOrConnectionString) {
            this.Configuration.AutoDetectChangesEnabled = AutoDetectChangesEnabled;
            this.Configuration.LazyLoadingEnabled = LazyLoadingEnabled;
            this.Configuration.ProxyCreationEnabled = ProxyCreationEnabled;
            this.Configuration.ValidateOnSaveEnabled = ValidateOnSaveEnabled;

            this.ObjectContext.ObjectMaterialized += OnObjectMaterialized;
        }
        #endregion

        #region Methods
        protected internal Dictionary<string, object> BuildOriginalValues(DbPropertyValues originalValues) {
            var result = new Dictionary<string, object>();
            foreach (var propertyName in originalValues.PropertyNames) {
                var value = originalValues[propertyName];
                if (value as DbPropertyValues != null) {
                    result[propertyName] = BuildOriginalValues(value as DbPropertyValues);
                }
                else {
                    result[propertyName] = value;
                }
            }
            return result;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {
            // Entity Framework Code First Conventions
            // https://msdn.microsoft.com/en-us/library/jj679962(v=vs.113).aspx
            // If a foreign key on the dependent entity is not nullable, then Code First sets cascade delete on the relationship. 
            // If a foreign key on the dependent entity is nullable, Code First does not set cascade delete on the relationship, 
            // and when the principal is deleted the foreign key will be set to null. 

            modelBuilder.Entity<Country>()
                .HasRequired(x => x.Continent)
                .WithMany(x => x.Countries)
                .HasForeignKey(x => x.ContinentId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<Country>()
                .HasOptional(x => x.CallingCode)
                .WithMany(x => x.Countries)
                .HasForeignKey(x => x.CallingCodeId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<Country>()
                .HasMany(x => x.Currencies)
                .WithMany(x => x.Countries)
                .Map(x => {
                    x.ToTable("CountryCurrency");
                    x.MapLeftKey("CountryId");
                    x.MapRightKey("CurrencyId");
                });

            modelBuilder.Entity<Currency>()
                .HasMany(x => x.Countries)
                .WithMany(x => x.Currencies)
                .Map(x => {
                    x.ToTable("CountryCurrency");
                    x.MapLeftKey("CurrencyId");
                    x.MapRightKey("CountryId");
                });

            modelBuilder.Entity<Country>()
                .HasMany(x => x.TimeZones)
                .WithMany(x => x.Countries)
                .Map(x => {
                    x.ToTable("CountryTimeZone");
                    x.MapLeftKey("CountryId");
                    x.MapRightKey("TimeZoneId");
                });

            modelBuilder.Entity<TimeZone>()
                .HasMany(x => x.Countries)
                .WithMany(x => x.TimeZones)
                .Map(x => {
                    x.ToTable("CountryTimeZone");
                    x.MapLeftKey("TimeZoneId");
                    x.MapRightKey("CountryId");
                });

            modelBuilder.Entity<CountryCurrencyInfo>().ToTable("CountryCurrencyInfo");

            modelBuilder.Entity<CountryInfo>().ToTable("CountryInfo");

            modelBuilder.Entity<CountryTimeZoneInfo>().ToTable("CountryTimeZoneInfo");

        }

        /// <summary>
        /// When an entity is materialized from the database, builds the collection of OriginalValues for concurrency purposes
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The ObjectMaterializedEventArgs event arguments</param>
        private void OnObjectMaterialized(object sender, ObjectMaterializedEventArgs e) {
            var entity = e.Entity as IObjectWithState;
            if (entity != null) {
                var changeTrackerEntry = this.Entry(entity);

                // .AsNoTracking() entities are detached/will not have OriginalValues
                if ((changeTrackerEntry != null) && (changeTrackerEntry.State != EntityState.Detached)) {
                    if (changeTrackerEntry.OriginalValues != null) {
                        if (changeTrackerEntry.State == EntityState.Modified) {

                            var modified = false;
                            foreach (var propertyName in changeTrackerEntry.OriginalValues.PropertyNames) {
                                if ((changeTrackerEntry.OriginalValues[propertyName] == null) && (changeTrackerEntry.CurrentValues[propertyName] == null)) continue;
                                if ((changeTrackerEntry.OriginalValues[propertyName] != null) && (changeTrackerEntry.CurrentValues[propertyName] != null)) {
                                    if (changeTrackerEntry.OriginalValues[propertyName].Equals(changeTrackerEntry.CurrentValues[propertyName])) continue;
                                }
                                modified = true;
                                break;
                            }

                            if (!modified) {
                                //DatabaseLog.Debug($"[ThreadId: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} Entity type: {entity.GetType()}; Id: {entity.EntityKey}; Resetting state to Unchanged to due it materialized in the Modified state, but all Original and Current values are the same.");
                                changeTrackerEntry.State = EntityState.Unchanged;
                            }
                        }

                        // re-check
                        if (changeTrackerEntry.State == EntityState.Modified) {
                            throw new ApplicationException("Entity materialized in the Modified state.");
                        }

                        if ((changeTrackerEntry.State != EntityState.Added) && (changeTrackerEntry.State != EntityState.Deleted)) {
                            entity.DetachedState = DetachedState.Unchanged;
                            entity.StartingOriginalValues = BuildOriginalValues(changeTrackerEntry.OriginalValues);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
