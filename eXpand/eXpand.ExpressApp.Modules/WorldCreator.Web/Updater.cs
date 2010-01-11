using System;
using System.Collections.Generic;
using System.Web;
using DevExpress.ExpressApp.Updating;
using DevExpress.Xpo;

namespace eXpand.ExpressApp.WorldCreator.Web {

    public class Updater : ModuleUpdater
    {
        public Updater(Session session, Version currentDBVersion) : base(session, currentDBVersion) { }
        public override void UpdateDatabaseAfterUpdateSchema()
        {
            var aspect = new TraceAspect(info => info.TargetInstance is IHttpHandler, @"C:\log.txt");
            base.UpdateDatabaseAfterUpdateSchema();
        }
    }
}