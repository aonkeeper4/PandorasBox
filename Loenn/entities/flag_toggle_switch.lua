local flagToggleSwitch = {}

flagToggleSwitch.name = "pandorasBox/flagToggleSwitch"
flagToggleSwitch.depth = 2000
flagToggleSwitch.placements = {
    default = {
        data = {
            onlyOff = false,
            onlyOn = false,
            silent = false,
            flag = ""
        }
    },

    {
        name = "activate",
        data = {
            onlyOn = true,
        }
    },
    {
        name = "deactivate",
        data = {
            onlyOff = true,
        }
    },
    {
        name = "toggle"
    }
}

function flagToggleSwitch.texture(room, entity)
    local onlyOff = entity.onlyOff
    local onlyOn = entity.onlyOn

    if onlyOff then
        return "objects/pandorasBox/flagToggleSwitch/switch13"

    elseif onlyOn then
        return "objects/pandorasBox/flagToggleSwitch/switch15"

    else
        return "objects/pandorasBox/flagToggleSwitch/switch01"
    end
end

return flagToggleSwitch