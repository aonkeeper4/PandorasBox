local gate = {}

gate.name = "pandorasBox/gate"
gate.depth = -9000
gate.placements = {
    name = "gate",
    data = {
        inverted = false,
        flag = "",
        texture = "objects/pandorasBox/gate/"
    }
}

gate.justification = {0.0, 0.0}
gate.offset = {0, -4}

function gate.texture(room, entity)
    local texture = entity.texture or "objects/pandorasBox/gate/"

    return texture .. "gate0"
end

return gate