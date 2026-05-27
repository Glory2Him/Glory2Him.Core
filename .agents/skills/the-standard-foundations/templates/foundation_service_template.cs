// ---
// skill: the-standard-foundations
// type: template
// source-section: "2.1 Foundation Services"
// ---

// ---------------------------------------------------------------
// Copyright (c) Coalition of the Good-Hearted Engineers
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System;
using System.Linq;
using System.Threading.Tasks;

public partial class {Entity}Service : I{Entity}Service
{
    private readonly IStorageBroker storageBroker;
    private readonly ILoggingBroker loggingBroker;
    private readonly IDateTimeBroker dateTimeBroker;

    public {Entity}Service(
        IStorageBroker storageBroker,
        ILoggingBroker loggingBroker,
        IDateTimeBroker dateTimeBroker)
    {
        this.storageBroker = storageBroker;
        this.loggingBroker = loggingBroker;
        this.dateTimeBroker = dateTimeBroker;
    }

    public ValueTask<{Entity}> Add{Entity}Async({Entity} {entity}) =>
        TryCatch(async () =>
        {
            Validate{Entity}OnAdd({entity});

            return await this.storageBroker.Insert{Entity}Async({entity});
        });

    public ValueTask<IQueryable<{Entity}>> RetrieveAll{Entity}s() =>
        TryCatch(async () => await this.storageBroker.SelectAll{Entity}s());

    public ValueTask<{Entity}> Retrieve{Entity}ByIdAsync(Guid {entity}Id) =>
        TryCatch(async () =>
        {
            Validate{Entity}Id({entity}Id);
            {Entity} maybe{Entity} = await this.storageBroker.Select{Entity}ByIdAsync({entity}Id);
            Validate{Entity}(maybe{Entity}, {entity}Id);

            return maybe{Entity};
        });

    public ValueTask<{Entity}> Modify{Entity}Async({Entity} {entity}) =>
        TryCatch(async () =>
        {
            Validate{Entity}OnModify({entity});

            return await this.storageBroker.Update{Entity}Async({entity});
        });

    public ValueTask<{Entity}> Remove{Entity}ByIdAsync(Guid {entity}Id) =>
        TryCatch(async () =>
        {
            Validate{Entity}Id({entity}Id);

            return await this.storageBroker.Delete{Entity}ByIdAsync({entity}Id);
        });
}
