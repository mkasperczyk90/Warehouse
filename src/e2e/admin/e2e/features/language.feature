Feature: Interface language (English for the desk, switchable to Polish)
  As a warehouse desk manager
  I want the panel in English, switchable to Polish
  So that the interface reads naturally for whoever is at the desk

  Background:
    Given the manager opens the admin panel

  Scenario: Switching the interface to Polish and back
    Then the navigation shows "Stock view"
    When the manager switches the language
    Then the navigation shows "Stany"
    And the navigation shows "Dziś"
    When the manager switches the language
    Then the navigation shows "Stock view"
