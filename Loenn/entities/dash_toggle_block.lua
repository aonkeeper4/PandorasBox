local dashToggleBlock = {}

dashToggleBlock.name = "pandorasBox/dashToggleBlock"
dashToggleBlock.fillColor = {0.8, 0.3, 1.0, 0.4}
dashToggleBlock.borderColor = {0.8, 0.3, 1.0, 1.0}
dashToggleBlock.fieldInformation = {
    divisor = {
        fieldType = "integer",
    }
}
dashToggleBlock.placements = {
    name = "dash_toggle_block",
    data = {
        width = 8,
        height = 8,
        index = "0",
        divisor = 2
    }
}

return dashToggleBlock