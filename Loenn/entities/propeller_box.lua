local propellerBox = {}

local glidingModes = {
    {"Always", "Always"},
    {"After Using Charge", "AfterUse"},
    {"Never", "Never"}
}

propellerBox.name = "pandorasBox/propellerBox"
propellerBox.depth = 50
propellerBox.fieldInformation = {
    maxCharges = {
        fieldType = "integer",
    },
    flashUseColor = {
        fieldType = "color",
        allowXNAColors = true,
    },
    flashChargeColor = {
        fieldType = "color",
        allowXNAColors = true,
    },
    glideMode = {
        options = glidingModes,
        editable = false
    }
}
propellerBox.placements = {
    name = "propeller_box",
    data = {
        texture = "default",
        flashUseColor = "3F437C",
        flashChargeColor = "5A1C1C",
        maxCharges = 3,
        rechargeOnGround = true,
        glideMode = "AfterUse",
    }
}

propellerBox.justification = {0.5, 1.0}

function propellerBox.texture(room, entity)
    local texture = entity.texture or "default"

    return string.format("objects/pandorasBox/propellerBox/%s/default_charges00", texture)
end

return propellerBox