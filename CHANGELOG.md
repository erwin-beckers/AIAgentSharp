# Changelog

All notable changes to AIAgentSharp will be documented in this file.

## [1.0.10] - 2024-12-19 (work in progress)

### 🔄 Changed
- **LlmCommunicator Refactoring** - Completely refactored `LlmCommunicator` to be the single point of responsibility for all LLM calls
- **Streaming Architecture** - All LLM operations now go through streaming with consistent event emission
- **ChainStepExecutor & TreeOfThoughtsCommunicator** - Updated to use `LlmCommunicator` instead of direct `ILlmClient` calls
- **Improved Error Handling** - Centralized error handling and logging in `LlmCommunicator`
- **Test Suite Updates** - Updated all unit tests to work with the new `LlmCommunicator` architecture


### 🎉 Added
- **Fluent API** - Intuitive agent configuration with method chaining
- **Fluent Event Handling** - Type-safe event configuration
- **Build Automation** - Automated development vs publishing workflows
- **Enhanced Documentation** - Complete guides and examples

### 🔄 Changed
- All examples now use fluent API by default
- Documentation overhaul with modern API patterns

