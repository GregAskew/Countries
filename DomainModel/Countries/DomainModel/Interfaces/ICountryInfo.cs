namespace Countries.DomainModel {

    public interface ICountryInfo {

        int? CallingCodeNumber { get; }
        string Capital { get; }
        string ContinentName { get; }
        int Id { get; }
        string ISO2 { get; }
        string ISO3 { get; }
        string ISOName { get; }
        string ISONumeric { get; }
        string Name { get; }
        string OfficialName { get; }

        string ToCSVString();
    }
}