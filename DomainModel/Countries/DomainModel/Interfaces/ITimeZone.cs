namespace Countries.DomainModel {

    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public interface ITimeZone {

        bool DST { get; set; }
        int Id { get; set; }
        byte[] RowVersion { get; set; }
        string TimeZoneAcronym { get; set; }
        string TimeZoneName { get; set; }
        decimal UTCOffset { get; set; }

        ICollection<Country> Countries { get; set; }

        string ToCSVString();
    }
}