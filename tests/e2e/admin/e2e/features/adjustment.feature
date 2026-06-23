Feature: Stock adjustment (UC-08)
  As a warehouse manager
  I want to post a manual adjustment with a reason
  So that damage or loss is corrected on the immutable ledger, audited

  Background:
    Given the manager opens the stock adjustment

  Scenario: The form seeds from the draft and computes the delta
    Then the stock adjustment is shown
    When the manager sets the counted quantity to "588"
    Then the delta "-12" is shown

  Scenario: A negative result is refused — stock can never go below zero
    When the manager sets the counted quantity to "-5"
    And the manager picks the adjustment reason "damage"
    And the manager posts the adjustment
    Then the below-zero error is shown
    And the adjustment is not posted

  Scenario: A valid adjustment is confirmed before it posts to the ledger
    When the manager sets the counted quantity to "588"
    And the manager picks the adjustment reason "damage"
    And the manager posts the adjustment
    Then the confirm-post dialog is shown
    And the adjustment is not posted
    When the manager confirms the post
    Then the adjustment is posted to the ledger
