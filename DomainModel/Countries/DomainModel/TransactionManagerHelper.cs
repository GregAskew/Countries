namespace Countries.DomainModel {

    #region Usings
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using System.Transactions;
    #endregion

    internal class TransactionManagerHelper {

        /// <summary>
        /// TransactionScope inherits a *maximum* timeout from Machine.config.  
        /// There's no way to override it from code unless you use reflection. 
        /// </summary>
        /// <param name="timeout"></param>
        internal static void OverrideMaximumTimeout(TimeSpan timeout) {

            var type = typeof(TransactionManager);
            var cachedMaxTimeout = type.GetField("_cachedMaxTimeout", BindingFlags.NonPublic | BindingFlags.Static);
            cachedMaxTimeout.SetValue(null, true);

            var maximumTimeout = type.GetField("_maximumTimeout", BindingFlags.NonPublic | BindingFlags.Static);
            maximumTimeout.SetValue(null, timeout);
        }
    }
}
