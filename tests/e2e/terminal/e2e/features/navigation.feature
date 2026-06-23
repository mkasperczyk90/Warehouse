Feature: Bottom-nav tab navigation
  As a warehouse operator
  I want a persistent tab bar across the three top-level roots
  So that I can switch between Tasks, Scan and Look up at any time

  Background:
    Given the operator opens the terminal

  Scenario: Switching between the three tabs
    When the operator opens the "Scan" tab
    Then the Scan screen is shown
    When the operator opens the "Look up" tab
    Then the Look up screen is shown
    When the operator opens the "Tasks" tab
    Then the task hub is shown again

  Scenario: The "More" tab is not yet available
    Then the "More" tab is disabled
