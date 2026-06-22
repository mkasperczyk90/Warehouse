Feature: User profile
  As a signed-in desk user
  I want to see and adjust my profile
  So that the panel reflects who I am and my preferences

  Background:
    Given the manager opens the profile screen

  Scenario: The profile shows the signed-in user
    Then "k.manager@warehouse.example" is shown
    And "Warehouse manager" is shown

  Scenario: Editing a preference and saving it
    When the manager sets the phone to "+48 600 999 999"
    And the manager saves the profile
    Then the profile shows it was saved

  Scenario: Reaching the profile from the user menu
    Given the manager opens the admin panel
    When the manager opens their profile from the user menu
    Then the URL is "/profile"
