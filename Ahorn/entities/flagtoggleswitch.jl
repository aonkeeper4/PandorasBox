module PandorasBoxFlagToggleSwitch

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/flagToggleSwitch" FlagToggleSwitch(x::Integer, y::Integer, onlyOff::Bool=false, onlyOn::Bool=false, silent::Bool=false, flag::String="")

const placements = Ahorn.PlacementDict(
    "Flag Toggle Switch (Activate) (Pandora's Box)" => Ahorn.EntityPlacement(
        FlagToggleSwitch,
        "point",
        Dict{String, Any}(
            "onlyOn" => true
        )
    ),
    "Flag Toggle Switch (Deactivate) (Pandora's Box)" => Ahorn.EntityPlacement(
        FlagToggleSwitch,
        "point",
        Dict{String, Any}(
            "onlyOff" => true
        )
    ),
    "Flag Toggle Switch (Pandora's Box)" => Ahorn.EntityPlacement(
        FlagToggleSwitch
    ),
)

function switchSprite(entity::FlagToggleSwitch)
    onlyOff = get(entity.data, "onlyOff", false)
    onlyOn = get(entity.data, "onlyOn", false)

    if onlyOff
        return "objects/pandorasBox/flagToggleSwitch/switch13"

    elseif onlyOn
        return "objects/pandorasBox/flagToggleSwitch/switch15"

    else
        return "objects/pandorasBox/flagToggleSwitch/switch01"
    end
end

function Ahorn.selection(entity::FlagToggleSwitch)
    x, y = Ahorn.position(entity)
    sprite = switchSprite(entity)

    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::FlagToggleSwitch, room::Maple.Room)
    sprite = switchSprite(entity)

    Ahorn.drawSprite(ctx, sprite, 0, 0)
end

end