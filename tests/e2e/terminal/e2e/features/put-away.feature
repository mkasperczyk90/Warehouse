Feature: Put away goods (UC-04)
  As a warehouse / forklift operator holding a pallet from the dock buffer
  I want the system to propose a legal location and prove it is compatible
  So that goods are stored only where temperature and capacity allow — never elsewhere

  Background:
    Given the operator opens the put-away screen

  Scenario: The system proposes a location and shows the hard checks
    Then the proposed location is "WH01-CR1-A03-R2-S1"
    And the pallet "4006381333931" is shown
    And the put-away check "Temperature compatible (cold room 2–6 °C)" passes
    And the put-away check "Capacity & load limit OK" passes

  Scenario: A full location proposes another legal bay
    When the operator reports the location full
    Then the proposed location is "WH01-CR1-A05-R1-S3"
    And the put-away check "Temperature compatible (cold room 2–6 °C)" passes
