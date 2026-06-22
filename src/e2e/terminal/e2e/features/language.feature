Feature: Interface language (Polish for the floor, switchable to English)
  As Polish warehouse floor staff
  I want the terminal in my language, switchable to English
  So that the interface reads naturally on the floor

  Background:
    Given the operator opens the terminal

  Scenario: Switching the interface to Polish and back
    Then the interface shows "Tasks assigned to you"
    When the operator switches the language
    Then the interface shows "Twoje zadania"
    And the task piles "Przyjęcie", "Odkładanie", "Kompletacja" and "Przesunięcie" are shown
    When the operator switches the language
    Then the interface shows "Tasks assigned to you"
