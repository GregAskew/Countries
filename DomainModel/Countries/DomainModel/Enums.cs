namespace Countries.DomainModel {

    /// <summary>
    /// Used to synchronize the ChangeTracker.EntityState when SaveChanges is called.
    /// </summary>
    public enum DetachedState {
        /// <summary>
        /// Modified entities use Unchanged.
        /// </summary>
        Unchanged,
        Added,
        Deleted,

        /// <summary>
        /// Do not use withchange-tracked entities.  For change-tracked entities that are Modified, use DetachedState.Unchanged
        /// During DetectChanges/ApplyPropertyChanges, change-tracked entities inspect the collection of saved properties to determine
        /// if an update is required, and updates only the properties that have changed.
        /// DetachedState.Modified is intended for use only when attaching an entity that was created with .NoTracking().
        /// Specifying DetachedState.Modified will send ALL of the properties to the database during an update (undesirable)
        /// </summary>
        Modified
    }
}
