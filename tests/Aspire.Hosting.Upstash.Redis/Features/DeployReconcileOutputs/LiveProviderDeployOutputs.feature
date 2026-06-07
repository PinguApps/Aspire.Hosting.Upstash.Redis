Feature: Live Upstash deploy and output behavior

  @live-upstash
  Scenario: Live deployment creates a disposable database
    Given a live disposable Upstash Redis deployment with prefix "pin-171-deploy"
    When the live Upstash deployment runs
    Then the live Upstash database exists with the configured name
    And the live Upstash database is registered for deletion

  @live-upstash
  Scenario: Live repeat deployment adopts the same disposable database
    Given a live disposable Upstash Redis deployment with prefix "pin-171-repeat"
    When the live Upstash deployment runs twice
    Then both live Upstash deployments returned the same provider id
    And only one live Upstash database exists with the configured name
    And the live Upstash database is registered for deletion

  @live-upstash
  Scenario: Live deployment populates app-facing outputs
    Given a live disposable Upstash Redis deployment with prefix "pin-171-output"
    When the live Upstash deployment runs
    Then the live Redis connection string matches the provider details
    And the live supplementary Upstash Redis outputs match the provider details
    And the live supplementary Upstash Redis password output is secret
    And the live Upstash database is registered for deletion
