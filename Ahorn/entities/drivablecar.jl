module PandorasBoxIntroCar

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/introCar" IntroCar(x::Integer, y::Integer, acceleration::Number=256.0, deceleration::Number=384.0, maxSpeed::Number=384.0, brokenDoor::Bool=false, keepCarSpeedOnExit::Bool=true, nitroAcceleration::Number=448.0, nitroMaxDuration::Number=3.0, nitroRegenMultiplier::Number=0.2, facing::Integer=1)

const placements = Ahorn.PlacementDict(
    "Intro Car (Pandora's Box)" => Ahorn.EntityPlacement(
        IntroCar
    )
)

Ahorn.editingOptions(entity::IntroCar) = Dict{String, Any}(
    "facing" => Dict{String, Any}(
        "Left" => -1,
        "Right" => 1
    )
)

const bodySprite = "scenery/car/body"
const wheelsSprite = "scenery/car/wheels"

function Ahorn.selection(entity::IntroCar)
    x, y = Ahorn.position(entity)

    return Ahorn.coverRectangles([
        Ahorn.getSpriteRectangle(bodySprite, x, y, jx=0.5, jy=1.0),
        Ahorn.getSpriteRectangle(wheelsSprite, x, y, jx=0.5, jy=1.0),
    ])
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::IntroCar, room::Maple.Room)
    x, y = Ahorn.position(entity)

    scaleX = get(entity.data, "facing", 1)

    Ahorn.drawSprite(ctx, wheelsSprite, x, y, jx=0.5, jy=1.0, sx=scaleX, sy=1)
    Ahorn.drawSprite(ctx, bodySprite, x, y, jx=0.5, jy=1.0, sx=scaleX, sy=1)
end

end