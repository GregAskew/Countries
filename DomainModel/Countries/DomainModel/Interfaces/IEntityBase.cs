namespace Countries.DomainModel {

    #region Usings
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Xml.Linq; 
    #endregion

    public interface IEntityBase : IObjectWithState, IValidatableObject {

        int GetMaxLength(string propertyName);
        bool StringPropertiesAreTrim();
        void TrimStringProperties();
    }
}
