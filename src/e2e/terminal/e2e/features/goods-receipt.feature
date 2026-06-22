Feature: Goods receipt (UC-02)
  As a warehouse operator receiving an announced delivery
  I want to count each ASN line against what was expected
  So that stock lands accurately in the dock buffer

  Background:
    Given the operator opens the goods receipt screen

  Scenario: The screen shows the ASN context and the expected line
    Then the ASN context "ASN-2206 · Dairy Farms Ltd" is shown
    And the dock "Dock D-3" is shown
    And the product "Whole milk 3.2% — 1 L carton" is shown
    And the counted quantity starts at the expected "240"

  Scenario: Adjusting the counted quantity with the stepper
    When the operator increases the count once
    Then the counted quantity is "241"
    When the operator decreases the count twice
    Then the counted quantity is "239"

  Scenario: Confirming a line returns to the task hub
    Given the operator started from the task hub
    When the operator confirms the line
    Then the task hub is shown again

  Scenario: Reporting a damaged-goods discrepancy still receives and drops the pile
    Given the operator started from the task hub
    When the operator reports a discrepancy "Damaged goods"
    Then the task hub is shown again
    And the "Receive" pile shows "1"
