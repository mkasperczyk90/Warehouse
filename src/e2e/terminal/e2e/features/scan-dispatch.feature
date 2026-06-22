Feature: Universal scan dispatch
  As a warehouse operator with a physical thing in my hand
  I want to scan any code and be routed to the matching task
  So that I never have to find the right screen myself

  Background:
    Given the operator opens the Scan tab

  Scenario: Scanning an ASN routes to goods receipt
    When the operator scans "ASN-2206"
    Then the result is recognised as an "Inbound ASN"
    And an action to open goods receipt is offered
    When the operator takes that action
    Then the goods receipt screen opens

  Scenario: Scanning an EAN offers a stock look-up
    When the operator scans "4006381333931"
    Then the result is recognised as a "Product (EAN)"
    And an action to look up stock is offered
    When the operator takes that action
    Then the look up screen opens

  Scenario: An unrecognised code offers no action
    When the operator scans "FOOBAR"
    Then the result is "Unrecognised code"
    And no action is offered
