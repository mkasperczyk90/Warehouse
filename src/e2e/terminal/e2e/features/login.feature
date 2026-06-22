Feature: Operator sign-in (badge scan)
  As a warehouse operator
  I want to badge in on the handheld
  So that only authorised staff start a shift on the terminal

  @anonymous
  Scenario: A known badge signs the operator in
    Given the terminal is at the sign-in screen
    When the operator scans the badge "7700"
    Then the task hub is shown again

  @anonymous
  Scenario: The hub greets the operator who actually badged in
    Given the terminal is at the sign-in screen
    When the operator scans the badge "7701"
    Then the hub greets "J. Forklift"

  @anonymous
  Scenario: An unknown badge is rejected and stays on sign-in
    Given the terminal is at the sign-in screen
    When the operator scans the badge "0000"
    Then the sign-in error is shown
    And the sign-in screen is shown

  Scenario: A signed-in operator can sign out
    Given the operator opens the terminal
    When the operator signs out
    Then the sign-in screen is shown
