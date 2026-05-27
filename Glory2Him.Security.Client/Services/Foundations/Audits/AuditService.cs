// ---------------------------------------------------------
// Copyright (c) North East London ICB. All rights reserved.
// ---------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Glory2Him.Security.Client.Brokers.DateTimes;
using Glory2Him.Security.Client.Models.Clients;
using Glory2Him.Security.Client.Models.Foundations.Audits.Exceptions;

namespace Glory2Him.Security.Client.Services.Foundations.Audits
{
    internal partial class AuditService : IAuditService
    {
        private readonly IDateTimeBroker dateTimeBroker;

        public AuditService(IDateTimeBroker dateTimeBroker) =>
            this.dateTimeBroker = dateTimeBroker;

        public ValueTask<T> ApplyAddAuditValuesAsync<T>(
            T entity,
            string userId,
            SecurityConfigurations securityConfigurations) =>
        TryCatch(async () =>
        {
            ValidateInputs(entity, userId, securityConfigurations);
            var auditDateTimeOffset = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            SetProperty(
                entity,
                propertyName: securityConfigurations.CreatedByPropertyName,
                value: userId);

            SetProperty(
                entity,
                propertyName: securityConfigurations.CreatedDatePropertyName,
                value: auditDateTimeOffset);

            SetProperty(
                entity,
                propertyName: securityConfigurations.UpdatedByPropertyName,
                value: userId);

            SetProperty(
                entity,
                propertyName: securityConfigurations.UpdatedDatePropertyName,
                value: auditDateTimeOffset);

            return entity;
        });

        public ValueTask<T> ApplyModifyAuditValuesAsync<T>(
            T entity,
            string userId,
            SecurityConfigurations securityConfigurations) =>
        TryCatch(async () =>
        {
            ValidateInputs(entity, userId, securityConfigurations);
            var auditDateTimeOffset = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();
            var updatedByName = securityConfigurations.UpdatedByPropertyName;
            var updatedDateName = securityConfigurations.UpdatedDatePropertyName;

            SetProperty(
                entity,
                propertyName: updatedByName,
                value: userId);

            SetProperty(
                entity,
                propertyName: updatedDateName,
                value: auditDateTimeOffset);

            return entity;
        });

        public ValueTask<T> ApplyRemoveAuditValuesAsync<T>(
            T entity,
            string userId,
            SecurityConfigurations securityConfigurations) =>
        TryCatch(async () =>
        {
            ValidateInputs(entity, userId, securityConfigurations);
            var auditDateTimeOffset = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            SetProperty(
                entity,
                propertyName: securityConfigurations.UpdatedByPropertyName,
                value: userId);

            SetProperty(
                entity,
                propertyName: securityConfigurations.UpdatedDatePropertyName,
                value: auditDateTimeOffset);

            return entity;
        });

        public ValueTask<T> EnsureAddAuditValuesRemainsUnchangedOnModifyAsync<T>(
            T entity,
            T storageEntity,
            SecurityConfigurations securityConfigurations) =>
        TryCatch<T>(async () =>
        {
            ValidateInputs(entity, storageEntity, securityConfigurations);
            var createdByName = securityConfigurations.CreatedByPropertyName;
            var createdDateName = securityConfigurations.CreatedDatePropertyName;
            object createdByValue = GetProperty(storageEntity, createdByName);
            object createdDateValue = GetProperty(storageEntity, createdDateName);
            SetProperty(entity, createdByName, createdByValue);
            SetProperty(entity, createdDateName, createdDateValue);

            return entity;
        });

        private object GetProperty<T>(T obj, string propertyName)
        {
            if (obj is IDictionary<string, object> expando)
            {
                if (!expando.TryGetValue(propertyName, out var value))
                {
                    throw new InvalidArgumentAuditException(
                        $"Property '{propertyName}' not found on storage ExpandoObject.");
                }

                return value;
            }

            var prop = typeof(T).GetProperty(propertyName);

            if (prop == null || !prop.CanRead)
            {
                throw new InvalidArgumentAuditException(
                    $"Property '{propertyName}' not found or not readable on storage type '{typeof(T).Name}'.");
            }

            return prop.GetValue(obj);
        }

        private static void SetProperty<T>(T entity, string propertyName, object value)
        {
            if (entity == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            if (entity is IDictionary<string, object> expando)
            {
                expando[propertyName] = value;
            }
            else
            {
                var property = entity.GetType().GetProperty(propertyName);

                if (property == null || !property.CanWrite)
                {
                    return;
                }

                var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                if (value != null && !targetType.IsAssignableFrom(value.GetType()))
                {
                    value = Convert.ChangeType(value, targetType);
                }

                property.SetValue(entity, value);
            }
        }
    }
}
