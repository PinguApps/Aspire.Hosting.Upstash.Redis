Feature: AppHost snippets

  Scenario: Sample AppHost snippets compile against the public API
    When the sample AppHost snippets are loaded
    Then the sample AppHost snippets cover the documented usage patterns

  Scenario: TypeScript demo AppHost uses the documented generated API
    When the TypeScript demo AppHost source is loaded
    Then the TypeScript demo AppHost uses the documented generated API
