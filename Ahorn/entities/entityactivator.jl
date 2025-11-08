module PandorasBoxEntityActivator

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/entityActivator" EntityActivator(x::Integer, y::Integer, mode::String="ActivateInsideDeactivateOutside", activationMode::String="OnEnter", targets::String="", useTracked::Bool=true, flag::String="", updateInterval::Number=-1.0)

const placements = Ahorn.PlacementDict(
    "Entity Activator (Pandora's Box)" => Ahorn.EntityPlacement(
        EntityActivator,
        "rectangle"
    )
)

const modes = String[
    "ActivateInsideDeactivateOutside",
    "ActivateInside",
    "DeactivateInside",
    "ActivateOutside",
    "DeactivateOutside",
    "ActivateOnScreenDeactivateOffScreen"
]

const activationModes = String[
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
]

Ahorn.editingOptions(entity::EntityActivator) = Dict{String, Any}(
    "mode" => modes,
    "activationMode" => activationModes
)

Ahorn.minimumSize(entity::EntityActivator) = 8, 8
Ahorn.resizable(entity::EntityActivator) = true, true

Ahorn.selection(entity::EntityActivator) = Ahorn.getEntityRectangle(entity)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::EntityActivator, room::Maple.Room)
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    Ahorn.drawRectangle(ctx, 0, 0, width, height, (0.7, 1.0, 0.7, 0.4), (0.7, 1.0, 0.7, 1.0))
end

end