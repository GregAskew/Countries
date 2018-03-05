namespace Countries.DomainModel.Tests {

    #region Usings
    using Extensions;
    using log4net;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks; 
    #endregion

    [TestClass]
    public class BaseTestClass {

        protected static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected List<ValidationResult> Get_Validation_Results<TEntity>(TEntity entity) where TEntity : class, IValidatableObject, IObjectWithState {
            var validationResults = entity.Validate(null).ToList();

            if (validationResults.Count > 0) {
                Log.Warn($"[ThreadID: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()} Type: {entity.GetType().Name} Validation errors:");

                foreach (var result in validationResults) {
                    Log.Warn($"[ThreadID: {Thread.CurrentThread.ManagedThreadId}] {ObjectExtensions.CurrentMethodName()}  -Property(s): {result.MemberNames.ToList().ToDelimitedString()} Validation Error Message: {result.ErrorMessage}");
                }
            }

            return validationResults;
        }
    }
}
