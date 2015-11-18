﻿using System;
using Cassandra;
using Cassandra.Data.Linq;
using Cassandra.Mapping;
using Skimur.Logging;

namespace Skimur.Cassandra.Migrations.DB
{
    internal class Versioner : IVersioner
    {
        private global::Cassandra.Mapping.IMapper _mapper;
        private readonly ILogger<Version> _logger;
        private static bool _didRegisterMapping = false;
        private static object _registrationLock = new object();

        public Versioner(ISession session, ILogger<Version> logger)
        {
            _logger = logger;

            _logger.Debug("Define global mappings");
            lock (_registrationLock)
            {
                if (!_didRegisterMapping)
                {
                    MappingConfiguration.Global.Define<PocoMapper>();
                    _didRegisterMapping = true;
                }
            }
            
            _logger.Debug("Create mapper and table instances");
            _mapper = new global::Cassandra.Mapping.Mapper(session);
            var table = new Table<DatabaseVersion>(session);
            table.CreateIfNotExists();
        }

        public int CurrentVersion(MigrationType type)
        {
            var dbVersion = _mapper.FirstOrDefault<DatabaseVersion>("WHERE type = ?", (int)type);

            if (dbVersion == null)
            {
                _logger.Info("No entries in database version table. Defaulting version value to 0.");
                return 0;
            }

            return dbVersion.Version;
        }

        public bool SetVersion(Migration migration)
        {
            try
            {
                _logger.Debug("Updating database version to " + migration.Version);
                _mapper.Insert(new DatabaseVersion
                {
                    Type = migration.Type,
                    Version = migration.Version,
                    Description = migration.GetDescription(),
                    Timestamp = DateTime.Now.ToUnixTime()
                });
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to execute insert of migration details into database version table", ex);
                return false;
            }

            return true;
        }
    }
}