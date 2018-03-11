namespace Countries.DomainModel {

    using System.Collections.Generic;

    public interface ICountry : IEntityBase {

        int Id { get; set; }
        string ISO2 { get; set; }
        string ISO3 { get; set; }
        string ISOName { get; set; }
        string ISONumeric { get; set; }
        string Name { get; set; }
        string OfficialName { get; set; }
        string OfficialNameLocal { get; set; }
        byte[] RowVersion { get; set; }

        CallingCode CallingCode { get; set; }
        int? CallingCodeId { get; set; }
        string Capital { get; set; }
        Continent Continent { get; set; }
        int ContinentId { get; set; }
        ICollection<Currency> Currencies { get; set; }

        string ToCSVString();
    }
}