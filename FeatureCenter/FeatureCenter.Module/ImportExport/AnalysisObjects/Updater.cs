﻿using System;
using System.IO;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using Xpand.ExpressApp.IO.Core;

using Xpand.Xpo;

namespace FeatureCenter.Module.ImportExport.AnalysisObjects {
    public class Updater : ModuleUpdater {
        public Updater(ObjectSpace objectSpace, Version currentDBVersion) : base(objectSpace, currentDBVersion) {
        }

        public override void UpdateDatabaseAfterUpdateSchema() {
            base.UpdateDatabaseAfterUpdateSchema();
            Import();
        }

        void Import() {
            if (ObjectSpace.Session.FindObject<Analysis>(analysis => analysis.Name == "Controlling Grid Settings") == null) {
                var importEngine = new ImportEngine();
                using (var unitOfWork = new UnitOfWork(ObjectSpace.Session.DataLayer)) {
                    Stream stream = GetType().Assembly.GetManifestResourceStream(GetType(), "AnalysisObjects.xml");
                    importEngine.ImportObjects(stream, unitOfWork);
                    stream = GetType().Assembly.GetManifestResourceStream(GetType(), "AnalysisObjectsConfiguration.xml");
                    importEngine.ImportObjects(stream, unitOfWork);
                }
            }
        }

    }
}