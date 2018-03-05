namespace Countries.DomainModel {

    using System.Collections.Generic;

    public interface ICallingCode : IEntityBase {

        int CallingCodeNumber { get; set; }
        int Id { get; set; }

        ICollection<Country> Countries { get; set; }

        string ToCSVString();
    }
}