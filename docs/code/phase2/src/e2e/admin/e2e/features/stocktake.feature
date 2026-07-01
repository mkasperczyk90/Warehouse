Feature: Stocktake (UC-07)
  As a warehouse manager
  I want to start blind counts and approve the differences
  So that the system stock is reconciled against a physical count, with an audit trail

  Scenario: Stocktakes are listed with a start-count affordance
    Given the manager opens the stocktakes
    Then the stocktake list is shown
    And "ST-118" is shown
    And "Cold room 1, aisle A" is shown

  Scenario: Starting a count opens the blind-count dialog
    Given the manager opens the stocktakes
    When the manager starts a count
    Then the start-count dialog is shown

  Scenario: The review shows the differences awaiting approval
    Given the manager opens the stocktake review "ST-118"
    Then "Stocktake ST-118 — Cold room 1, aisle A" is shown
    And "Butter block 250 g · LOT-0331" is shown

  Scenario: Every selected difference needs a reason before approval
    Given the manager opens the stocktake review "ST-118"
    Then the approve-differences button is enabled
    When the manager selects the difference at "CR1-A03-R2-S3"
    Then the approve-differences button is disabled
    When the manager sets the reason at "CR1-A03-R2-S3" to "loss"
    Then the approve-differences button is enabled

  Scenario: Approving posts the differences to the ledger
    Given the manager opens the stocktake review "ST-118"
    When the manager approves the differences
    Then the differences are posted to the ledger
