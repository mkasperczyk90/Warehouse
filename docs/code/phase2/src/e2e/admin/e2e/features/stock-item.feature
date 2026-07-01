Feature: Stock item — move and block (UC-06 Move stock)
  As a warehouse manager
  I want to drill into a stock item and move or block it
  So that I can relocate stock safely or quarantine it when needed

  Scenario: The item drill-down shows breakdown, history and the adjust action (UC-05)
    Given the manager opens the stock item "5"
    Then "Cheese wheel 5 kg" is shown
    And "Blocked · QC" is shown
    And "Movement history" is shown

  Scenario: A move to an incompatible room is refused (environment invariant)
    Given the manager opens the stock item "1"
    When the manager opens the move dialog
    Then the confirm-move button is disabled
    When the manager picks the target location "A2-A07-R3-S2"
    Then the move is flagged incompatible
    And the confirm-move button is disabled
    When the manager picks the target location "CR1-A01-R1-S4"
    Then the confirm-move button is enabled

  Scenario: Blocking a stock item requires a reason (UC-03 entry)
    Given the manager opens the stock item "2"
    When the manager opens the block dialog
    Then the block-confirm button is disabled
    When the manager picks the block reason "damage"
    And the manager confirms the block
    Then "sent to quarantine" is shown
