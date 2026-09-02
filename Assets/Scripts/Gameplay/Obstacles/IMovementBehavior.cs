namespace Gameplay.Obstacles
{
    public interface IMovementBehavior
    {
        void Initialize(ObstacleGroup group);

        void Tick(float deltaTime);
    }
}
