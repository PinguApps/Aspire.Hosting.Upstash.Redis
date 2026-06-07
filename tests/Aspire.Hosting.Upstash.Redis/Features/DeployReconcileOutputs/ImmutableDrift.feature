Feature: Immutable Upstash Redis drift detection

  Scenario: Database name identity drift fails before mutation
    Given an existing Upstash Redis database detail named "renamed-cache" in region "eu-west-1" with TLS enabled
    When immutable drift is checked for requested database "orders-cache" with default options
    Then immutable drift detection fails because "DatabaseNameMismatch"
    And the immutable drift failure message contains "Requested database name 'orders-cache'"
    And the immutable drift failure message contains "returned name 'renamed-cache'"
    And the immutable drift failure message contains "will not rename or adopt a different resource automatically"
    And no unsafe provider mutation is attempted

  Scenario: Platform drift fails when provider state exposes the platform through primary region
    Given an existing Upstash Redis database detail named "orders-cache" in region "us-central1" with TLS enabled
    When immutable drift is checked for requested database "orders-cache" with platform "aws"
    Then immutable drift detection fails because "PlatformMismatch"
    And the immutable drift failure message contains "Requested platform 'aws'"
    And the immutable drift failure message contains "maps to platform 'gcp'"
    And the immutable drift failure message contains "Platform is create-time-only"
    And no unsafe provider mutation is attempted

  Scenario: Primary region drift fails for explicit requested primary region
    Given an existing Upstash Redis database detail named "orders-cache" in region "us-east-1" with TLS enabled
    When immutable drift is checked for requested database "orders-cache" with primary region "eu-west-1"
    Then immutable drift detection fails because "PrimaryRegionMismatch"
    And the immutable drift failure message contains "Requested primary region 'eu-west-1'"
    And the immutable drift failure message contains "found 'us-east-1'"
    And the immutable drift failure message contains "Primary region is create-time-only"
    And no unsafe provider mutation is attempted

  Scenario: Disabled remote TLS fails even when TLS was not explicit
    Given an existing Upstash Redis database detail named "orders-cache" in region "eu-west-1" with TLS disabled
    When immutable drift is checked for requested database "orders-cache" with default options
    Then immutable drift detection fails because "TlsDisabled"
    And the immutable drift failure message contains "Requested TLS 'required enabled'"
    And the immutable drift failure message contains "found 'disabled'"
    And the immutable drift failure message contains "will not call provider TLS repair endpoints automatically"
    And no unsafe provider mutation is attempted

  Scenario: Mutable setting differences do not fail immutable drift detection
    Given an existing Upstash Redis database detail named "orders-cache" in region "eu-west-1" with TLS enabled
    When immutable drift is checked for requested database "orders-cache" with mutable settings
    Then immutable drift detection succeeds
    And no unsafe provider mutation is attempted
