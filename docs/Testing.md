# Plugin component tests

PluginScenario invokes the public IPlugin.Execute entry point with the generated PAC runtime,
registration guards, real DI scope, application services and repositories. Mock only the
IOrganizationService boundary; do not replace application services in these tests.

Arrange message, entity, stage, mode, Target and named images in the scenario. Set SDK
expectations through Platform.Organization (strict Moq). Execute the actual plugin instance.
PluginRun exposes independent Target snapshots before/after, Failure, ordered SDK method names
and runtime traces. Execution captures exceptions so failure tests can inspect all effects;
always assert Failure (null or the expected exception). Outputs and shared variables are
available on Platform. Verify SDK arguments and call counts explicitly, then VerifyNoOtherCalls.

AccountPluginTests exercise the shipped Create/Update plugins with real AccountService.
Normalization intentionally performs no database call. AccountServicePipelineTests use a
clearly test-only plugin to exercise the real service/repository read path, SDK projection,
fault propagation, output parameters and shared variables. This does not add a production step.
Existing focused unit tests remain useful for edge cases and scope disposal.

This is a component harness, not a Dataverse server emulator: it does not execute platform CRUD,
transactions, automatic step dispatch, filtering attributes, security, image creation or rollback.
Supply the context/images the platform would provide. Live environment tests are needed for
those server behaviors. The same suite also runs against the signed merged DLL via npm run check.
