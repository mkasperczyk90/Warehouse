Feature: Quality inspection (UC-03)
  As a quality inspector
  I want to release or reject quarantined batches with an audited reason
  So that blocked stock stays invisible to reservations until a decision is made

  Background:
    Given the inspector opens the quality holds

  Scenario: Quarantined batches awaiting a decision are listed
    Then the quality holds are shown
    And "Cheese wheel 5 kg" is shown

  Scenario: A decision requires a reason before it can be confirmed (UC-03 audit)
    When the inspector rejects the batch "Cheese wheel 5 kg"
    Then the confirm-reject button is disabled
    When the inspector picks the reason "damaged"
    Then the confirm-reject button is enabled

  Scenario: Releasing a batch removes it from the worklist
    When the inspector releases the batch "Cheese wheel 5 kg"
    And the inspector picks the reason "passed"
    And the inspector confirms the release
    Then the batch "Cheese wheel 5 kg" is no longer shown
