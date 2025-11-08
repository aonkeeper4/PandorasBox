local waterDrownController = {}

waterDrownController.name = "pandorasBox/waterDrowningController"
waterDrownController.depth = 0
waterDrownController.fieldInformation = {
    mode = {
        options = {
            "Swimming",
            "Diving"
        },
        editable = false
    }
}
waterDrownController.placements = {
    name = "controller",
    data = {
        mode = "Swimming",
        maxDuration = 10.0
    }
}

waterDrownController.texture = "objects/pandorasBox/controllerIcons/waterDrowningController"

return waterDrownController