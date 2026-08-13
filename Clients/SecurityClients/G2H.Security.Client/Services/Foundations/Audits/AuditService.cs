// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
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
                entity: entity,
                propertyName: securityConfigurations.CreatedByPropertyName,
                value: userId);

            SetProperty(
                entity: entity,
                propertyName: securityConfigurations.CreatedWhenPropertyName,
                value: auditDateTimeOffset);

            SetProperty(
                entity: entity,
                propertyName: securityConfigurations.UpdatedByPropertyName,
                value: userId);

            SetProperty(
                entity: entity,
                propertyName: securityConfigurations.UpdatedWhenPropertyName,
                value: auditDateTimeOffset);

            if (HasWritablePropertyOfType(
                entity: entity,
                propertyName: securityConfigurations.DeletedByPropertyName,
                expectedType: securityConfigurations.DeletedByPropertyType))
            {
                SetProperty(entity: entity, propertyName: securityConfigurations.DeletedByPropertyName, value: null);
            }

            if (HasWritablePropertyOfType(
                entity: entity,
                propertyName: securityConfigurations.DeletedWhenPropertyName,
                expectedType: securityConfigurations.DeletedWhenPropertyType))
            {
                SetProperty(
                    entity: entity,
                    propertyName: securityConfigurations.DeletedWhenPropertyName,
                    value: null);
            }

            if (HasWritablePropertyOfType(
                entity: entity,
                propertyName: securityConfigurations.IsDeletedPropertyName,
                expectedType: securityConfigurations.IsDeletedPropertyType))
            {
                SetProperty(
                    entity: entity,
                    propertyName: securityConfigurations.IsDeletedPropertyName,
                    value: false);
            }

            if (HasWritablePropertyOfType(
                entity: entity,
                propertyName: securityConfigurations.DeletionReasonPropertyName,
                expectedType: securityConfigurations.DeletionReasonPropertyType))
            {
                SetProperty(
                    entity: entity,
                    propertyName: securityConfigurations.DeletionReasonPropertyName,
                    value: null);
            }

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
                entity: entity,
                propertyName: updatedByName,
                value: userId);

            SetProperty(
                entity: entity,
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
                entity: entity,
                propertyName: securityConfigurations.DeletedByPropertyName,
                value: userId);

            SetProperty(
                entity: entity,
                propertyName: securityConfigurations.DeletedWhenPropertyName,
                value: auditDateTimeOffset);

            SetProperty(
                entity: entity,
                propertyName: securityConfigurations.IsDeletedPropertyName,
                value: true);

            // deletionReason is optional, so a null means "the caller did not supply one" rather
            // than "clear it" — writing it unconditionally would erase a reason the caller had
            // already put on the entity. The add path is where the reason gets reset.
            if (deletionReason is not null)
            {
                SetProperty(
                    entity: entity,
                    propertyName: securityConfigurations.DeletionReasonPropertyName,
                    value: deletionReason);
            }

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

            object? createdByValue = GetProperty(obj: storageEntity, propertyName: createdByName);
            object? createdWhenValue = GetProperty(obj: storageEntity, propertyName: createdWhenName);
            SetProperty(entity: entity, propertyName: createdByName, value: createdByValue);
            SetProperty(entity: entity, propertyName: createdWhenName, value: createdWhenValue);

            if (HasWritablePropertyOfType(
                entity: entity,
                propertyName: deletedByName,
                expectedType: securityConfigurations.DeletedByPropertyType))
            {
                SetProperty(
                    entity: entity,
                    propertyName: deletedByName,
                    value: GetProperty(obj: storageEntity, propertyName: deletedByName));
            }

            if (HasWritablePropertyOfType(
                entity: entity,
                propertyName: deletedWhenName,
                expectedType: securityConfigurations.DeletedWhenPropertyType))
            {
                SetProperty(
                    entity: entity,
                    propertyName: deletedWhenName,
                    value: GetProperty(obj: storageEntity, propertyName: deletedWhenName));
            }

            if (HasWritablePropertyOfType(
                entity: entity,
                propertyName: isDeletedName,
                expectedType: securityConfigurations.IsDeletedPropertyType))
            {
                SetProperty(
                    entity: entity,
                    propertyName: isDeletedName,
                    value: GetProperty(obj: storageEntity, propertyName: isDeletedName));
            }

            if (HasWritablePropertyOfType(
                entity: entity,
                propertyName: deletionReasonName,
                expectedType: securityConfigurations.DeletionReasonPropertyType))
            {
                SetProperty(
                    entity: entity,
                    propertyName: deletionReasonName,
                    value: GetProperty(obj: storageEntity, propertyName: deletionReasonName));
            }

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
            object? createdByValue = GetProperty(obj: storageEntity, propertyName: createdByName);
            object? createdWhenValue = GetProperty(obj: storageEntity, propertyName: createdWhenName);
            object? updatedByValue = GetProperty(obj: storageEntity, propertyName: updatedByName);
            object? updatedWhenValue = GetProperty(obj: storageEntity, propertyName: updatedWhenName);
            SetProperty(entity: entity, propertyName: createdByName, value: createdByValue);
            SetProperty(entity: entity, propertyName: createdWhenName, value: createdWhenValue);
            SetProperty(entity: entity, propertyName: updatedByName, value: updatedByValue);
            SetProperty(entity: entity, propertyName: updatedWhenName, value: updatedWhenValue);

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
