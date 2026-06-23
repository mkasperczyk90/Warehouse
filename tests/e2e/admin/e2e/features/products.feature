Feature: Manage products (UC-13)
  As a warehouse manager
  I want to browse the catalogue and create/edit products
  So that master data (SKU, storage requirements, tracking) is correct

  Scenario: The catalogue lists products with a create affordance
    Given the manager opens the product catalogue
    Then the product catalogue is shown
    And the product "Whole milk 3.2% — 1 L carton" is shown
    And the product "Cardboard box L" is shown

  Scenario: Filtering the catalogue by search text
    Given the manager opens the product catalogue
    When the manager searches products for "berries"
    Then the product "Frozen berries 1 kg" is shown
    And the product "Whole milk 3.2% — 1 L carton" is not shown

  Scenario: Filtering the catalogue by category
    Given the manager opens the product catalogue
    When the manager filters products by category "Packaging"
    Then the product "Cardboard box L" is shown
    And the product "Whole milk 3.2% — 1 L carton" is not shown

  Scenario: Editing a product seeds the form and saves
    Given the manager opens the product "4006381333931"
    Then the product name field shows "Whole milk 3.2% — 1 L carton"
    When the manager saves the product
    Then the product is saved

  Scenario: An inverted temperature range is rejected on save (UC-13 invariant)
    Given the manager opens the product "4006381333931"
    When the manager sets the minimum temperature to "10"
    And the manager saves the product
    Then the temperature-range error is shown
    And the product is not saved

  Scenario: Creating a product requires a valid SKU
    Given the manager opens the product catalogue
    When the manager starts a new product
    Then the new-product form is shown
    When the manager creates the product
    Then the SKU-length error is shown
