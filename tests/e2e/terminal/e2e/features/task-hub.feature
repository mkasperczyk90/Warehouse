Feature: Operator task hub
  As a warehouse operator on a rugged handheld terminal
  I want to see the tasks assigned to me the moment I open the app
  So that I can start the most urgent work with a single tap

  Background:
    Given the operator opens the terminal

  Scenario: The hub greets the operator and lists today's task piles
    Then the operator sees their name "M. Operator" and site "Cold-store · Wrocław WH-01"
    And the connectivity status shows "Online"
    And an always-focused scan field invites a scan
    And the task piles "Receive", "Put away", "Pick" and "Move stock" are shown

  Scenario: Tapping a task pile opens that workflow
    When the operator taps the "Receive" pile
    Then the goods receipt screen opens
