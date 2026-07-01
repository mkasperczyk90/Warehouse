Feature: Packing (UC-11)
  As a warehouse operator in the packing zone
  I want to pack picked items, record weight and dimensions, and label the package
  So that the order leaves as well-formed, labelled parcels

  Background:
    Given the operator opens the packing screen

  Scenario: The packing screen shows the active package and its contents
    Then the active package is "PKG 1 · carton M"
    And the packing detail "Greek yoghurt 400 g" is shown
    And the packing detail "14.8 kg" is shown
    And the packing detail "40×30×25 cm" is shown

  Scenario: Opening another package for the remainder
    When the operator opens another package
    Then the active package is "PKG 2 · carton M"
