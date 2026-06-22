Feature: Manage warehouse topology (UC-14)
  As a warehouse manager
  I want to browse rooms and locations and edit them
  So that the physical layout (rooms, types, locations) is modelled correctly

  Background:
    Given the manager opens the topology

  Scenario: The tree and the first room's detail are shown
    Then the topology is shown
    And "WH-01 Wrocław" is shown

  Scenario: Selecting a room switches the detail
    When the manager selects the room "Freezer 1"
    Then "Freezer 1 — WH-01" is shown
    And "FZ1-B02-R4-S1" is shown

  Scenario: Adding a location to a room
    When the manager adds the location "CR1-A05-R1-S1"
    Then "CR1-A05-R1-S1" is shown

  Scenario: Saving a room
    When the manager saves the room
    Then the room is saved
