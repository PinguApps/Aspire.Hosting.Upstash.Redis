Feature: AppHost snippets

  Scenario: Sample AppHost snippets compile against the public API
    When the sample AppHost snippets are loaded
    Then the sample AppHost snippets cover the documented usage patterns
