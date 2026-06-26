Feature: Manage products (UC-13)
  As a warehouse manager
  I want to browse the catalogue and define/maintain products
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
    When the manager filters products by category "Dry goods"
    Then the product "Cardboard box L" is shown
    And the product "Whole milk 3.2% — 1 L carton" is not shown

  Scenario: Renaming a product updates its detail (UC-13)
    Given the manager opens the product "MILK-1L"
    Then the product detail shows "Whole milk 3.2% — 1 L carton"
    When the manager renames the product to "Whole milk 1 L"
    Then the product detail shows "Whole milk 1 L"

  Scenario: An inverted temperature range is rejected (UC-13 invariant)
    Given the manager opens the product catalogue
    When the manager starts a new product
    And the manager fills in a valid SKU and name
    And the manager sets cold-chain storage with min "10" and max "2"
    And the manager creates the product
    Then the temperature-range error is shown

  Scenario: Creating a product requires a valid SKU
    Given the manager opens the product catalogue
    When the manager starts a new product
    Then the new-product form is shown
    When the manager creates the product
    Then the SKU-length error is shown
