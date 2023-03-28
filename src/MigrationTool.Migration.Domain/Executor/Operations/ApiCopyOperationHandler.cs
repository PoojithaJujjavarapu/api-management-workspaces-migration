﻿using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Operations;

namespace MigrationTool.Migration.Domain.Executor.Operations;

public class ApiCopyOperationHandler : OperationHandler
{
    private readonly ApiClient apiClient;
    private readonly EntitiesRegistry registry;

    public ApiCopyOperationHandler(ApiClient apiClient, EntitiesRegistry registry)
    {
        this.apiClient = apiClient;
        this.registry = registry;
    }

    public override EntityType UsedEntities => EntityType.Api;
    public override Type OperationType => typeof(CopyOperation);

    public override async Task Handle(IMigrationOperation operation, string workspaceId)
    {
        var copyOperation = this.GetOperationOrThrow<CopyOperation>(operation);

        var originalApi = copyOperation.Entity;
        var newApi = await this.apiClient.Create(originalApi, IdModifier, workspaceId);
        this.registry.RegisterMapping(originalApi, newApi);

        var apiPolicy = await this.apiClient.FetchPolicy(originalApi.Id);
        if (apiPolicy != null)
            await this.apiClient.UploadApiPolicy(newApi, apiPolicy, workspaceId);

        foreach (var originalOperation in await this.apiClient.FetchOperations(originalApi.Id))
        {
            var newOperation = await this.apiClient.CreateOperation(originalOperation, newApi, workspaceId);
            var policy = await this.apiClient.FetchOperationPolicy(originalApi.Id, originalOperation.Id);
            if (policy != null)
                await this.apiClient.UploadApiOperationPolicy(newApi, newOperation, policy, workspaceId);
        }

        string IdModifier(string id) => $"{id}-in-{workspaceId}";
    }
}