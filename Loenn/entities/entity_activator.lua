local entityActivator = {}

local modes = {
    "ActivateInsideDeactivateOutside",
    "ActivateInside",
    "DeactivateInside",
    "ActivateOutside",
    "DeactivateOutside",
    "ActivateOnScreenDeactivateOffScreen"
}

local activationModes = {
    "OnEnter",
    "OnStay",
    "OnLeave",
    "OnFlagActive",
    "OnFlagInactive",
    "OnFlagActivated",
    "OnFlagDeactivated",
    "OnUpdate",
    "OnAwake",
    "OnCameraMoved"
}

entityActivator.name = "pandorasBox/entityActivator"
entityActivator.fillColor = {0.7, 1.0, 0.7, 0.4}
entityActivator.borderColor = {0.7, 1.0, 0.7, 1.0}
entityActivator.fieldInformation = {
    mode = {
        options = modes,
        editable = false
    },
    activationMode = {
        options = activationModes,
        editable = false
    },
}
entityActivator.placements = {
    name = "entity_activator",
    data = {
        width = 8,
        height = 8,
        mode = "ActivateInsideDeactivateOutside",
        activationMode = "OnEnter",
        targets = "",
        useTracked = true,
        flag = "",
        updateInterval = -1.0,
        changeCollision = true,
        changeActive = true,
        changeVisible = true,
        affectComponents = true,
        cacheTargets = true
    }
}

return entityActivator