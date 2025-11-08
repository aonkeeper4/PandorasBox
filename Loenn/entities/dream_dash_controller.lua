local dreamDashController = {}

dreamDashController.name = "pandorasBox/dreamDashController"
dreamDashController.depth = 0
dreamDashController.fieldInformation = {
    activeBackColor = {
        fieldType = "color",
        allowXNAColors = true,
    },
    disabledBackColor = {
        fieldType = "color",
        allowXNAColors = true,
    },
    activeLineColor = {
        fieldType = "color",
        allowXNAColors = true,
    },
    disabledLineColor = {
        fieldType = "color",
        allowXNAColors = true,
    }
}
dreamDashController.placements = {
    name = "controller",
    data = {
        allowSameDirectionDash = false,
        allowDreamDashRedirect = true,
        overrideDreamDashSpeed = false,
        neverSlowDown = false,
        useEntrySpeedAngle = false,
        bounceOnCollision = false,
        stickOnCollision = false,
        overrideColors = false,
        sameDirectionSpeedMultiplier = 1.0,
        dreamDashSpeed = 240.0,
        activeBackColor = "Black",
        disabledBackColor = "af2e2d",
        activeLineColor = "White",
        disabledLineColor = "6a8480",
        particleLayer0Colors = "ffef11,ff00d0,08a310",
        particleLayer1Colors = "5fcde4,7fb25e,e0564c",
        particleLayer2Colors = "5b6ee1,CC3B3B,7daa64"
    }
}



dreamDashController.texture = "objects/pandorasBox/controllerIcons/dreamDashController"

return dreamDashController