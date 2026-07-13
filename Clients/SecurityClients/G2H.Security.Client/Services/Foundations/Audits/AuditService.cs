// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’" 
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6 
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using G2H.Security.Client.Brokers.DateTimes;
using G2H.Security.Client.Models.Clients;
using G2H.Security.Client.Models.Foundations.Audits.Exceptions;

namespace G2H.Security.Client.Services.Foundations.Audits
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
            ValidateOnApplyAddAuditValues(entity, userId, securityConfigurations);
            var auditDateTimeOffset = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            SetProperty(
                entity,
                propertyName: securityConfigurations.CreatedByPropertyName,
                value: userId);

            SetProperty(
                entity,
                propertyName: securityConfigurations.CreatedWhenPropertyName,
                value: auditDateTimeOffset);

            SetProperty(
                entity,
                propertyName: securityConfigurations.UpdatedByPropertyName,
                value: userId);

            SetProperty(
                entity,
                propertyName: securityConfigurations.UpdatedWhenPropertyName,
                value: auditDateTimeOffset);

            if (HasWritablePropertyOfType(entity, securityConfigurations.DeletedByPropertyName, securityConfigurations.DeletedByPropertyType))
                SetProperty(entity, securityConfigurations.DeletedByPropertyName, null);

            if (HasWritablePropertyOfType(entity, securityConfigurations.DeletedWhenPropertyName, securityConfigurations.DeletedWhenPropertyType))
                SetProperty(entity, securityConfigurations.DeletedWhenPropertyName, null);

            if (HasWritablePropertyOfType(entity, securityConfigurations.IsDeletedPropertyName, securityConfigurations.IsDeletedPropertyType))
                SetProperty(entity, securityConfigurations.IsDeletedPropertyName, false);

            if (HasWritablePropertyOfType(entity, securityConfigurations.DeletionReasonPropertyName, securityConfigurations.DeletionReasonPropertyType))
                SetProperty(entity, securityConfigurations.DeletionReasonPropertyName, null);

            return entity;
        });

        public ValueTask<T> ApplyModifyAuditValuesAsync<T>(
            T entity,
            string userId,
            SecurityConfigurations securityConfigurations) =>
        TryCatch(async () =>
        {
            ValidateOnApplyModifyAuditValues(entity, userId, securityConfigurations);
            var auditDateTimeOffset = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();
            var updatedByName = securityConfigurations.UpdatedByPropertyName;
            var updatedDateName = securityConfigurations.UpdatedWhenPropertyName;

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
            SecurityConfigurations securityConfigurations,
            string? deletionReason) =>
        TryCatch(async () =>
        {
            ValidateOnApplyRemoveAuditValues(entity, userId, securityConfigurations);
            var auditDateTimeOffset = await this.dateTimeBroker.GetCurrentDateTimeOffsetAsync();

            SetProperty(
                entity,
                propertyName: securityConfigurations.DeletedByPropertyName,
                value: userId);

            SetProperty(
                entity,
                propertyName: securityConfigurations.DeletedWhenPropertyName,
                value: auditDateTimeOffset);

            SetProperty(
                entity,
                propertyName: securityConfigurations.IsDeletedPropertyName,
                value: true);

            SetProperty(
                entity,
                propertyName: securityConfigurations.DeletionReasonPropertyName,
                value: deletionReason);

            return entity;
        });

        public ValueTask<T> EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync<T>(
            T entity,
            T storageEntity,
            SecurityConfigurations securityConfigurations) =>
        TryCatch<T>(async () =>
        {
            ValidateInputs(entity, storageEntity, securityConfigurations);
            var createdByName = securityConfigurations.CreatedByPropertyName;
            var createdWhenName = securityConfigurations.CreatedWhenPropertyName;
            var deletedByName = securityConfigurations.DeletedByPropertyName;
            var deletedWhenName = securityConfigurations.DeletedWhenPropertyName;
            var isDeletedName = securityConfigurations.IsDeletedPropertyName;
            var deletionReasonName = securityConfigurations.DeletionReasonPropertyName;
            object? createdByValue = GetProperty(storageEntity, createdByName);
            object? createdWhenValue = GetProperty(storageEntity, createdWhenName);
            SetProperty(entity, createdByName, createdByValue);
            SetProperty(entity, createdWhenName, createdWhenValue);

            if (HasWritablePropertyOfType(entity, deletedByName, securityConfigurations.DeletedByPropertyType))
                SetProperty(entity, deletedByName, GetProperty(storageEntity, deletedByName));

            if (HasWritablePropertyOfType(entity, deletedWhenName, securityConfigurations.DeletedWhenPropertyType))
                SetProperty(entity, deletedWhenName, GetProperty(storageEntity, deletedWhenName));

            if (HasWritablePropertyOfType(entity, isDeletedName, securityConfigurations.IsDeletedPropertyType))
                SetProperty(entity, isDeletedName, GetProperty(storageEntity, isDeletedName));

            if (HasWritablePropertyOfType(entity, deletionReasonName, securityConfigurations.DeletionReasonPropertyType))
                SetProperty(entity, deletionReasonName, GetProperty(storageEntity, deletionReasonName));

            return entity;
        });

        public ValueTask<T> EnsureOtherAuditValuesRemainsUnchangedOnRemoveAsync<T>(
            T entity,
            T storageEntity,
            SecurityConfigurations securityConfigurations) =>
        TryCatch<T>(async () =>
        {
            ValidateInputs(entity, storageEntity, securityConfigurations);
            var createdByName = securityConfigurations.CreatedByPropertyName;
            var createdWhenName = securityConfigurations.CreatedWhenPropertyName;
            var updatedByName = securityConfigurations.UpdatedByPropertyName;
            var updatedWhenName = securityConfigurations.UpdatedWhenPropertyName;
            object? createdByValue = GetProperty(storageEntity, createdByName);
            object? createdWhenValue = GetProperty(storageEntity, createdWhenName);
            object? updatedByValue = GetProperty(storageEntity, updatedByName);
            object? updatedWhenValue = GetProperty(storageEntity, updatedWhenName);
            SetProperty(entity, createdByName, createdByValue);
            SetProperty(entity, createdWhenName, createdWhenValue);
            SetProperty(entity, updatedByName, updatedByValue);
            SetProperty(entity, updatedWhenName, updatedWhenValue);

            return entity;
        });

        private object? GetProperty<T>(T obj, string propertyName)
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

        private static bool HasWritablePropertyOfType<T>(T entity, string propertyName, Type expectedType)
        {
            if (entity == null || string.IsNullOrWhiteSpace(propertyName) || expectedType == null)
                return false;

            if (entity is IDictionary<string, object> expandoCheck)
                return expandoCheck.ContainsKey(propertyName);

            var property = entity.GetType().GetProperty(propertyName);

            if (property == null || !property.CanWrite)
                return false;

            var underlyingType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            return underlyingType.IsAssignableFrom(expectedType);
        }

        private static void SetProperty<T>(T entity, string propertyName, object? value)
        {
            if (entity == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            if (entity is IDictionary<string, object> expando)
            {
                expando[propertyName] = value!;
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
