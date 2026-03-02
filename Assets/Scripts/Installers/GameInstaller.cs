using UnityEngine;
using Zenject;
using CubeTower.Config;
using CubeTower.Services;
using CubeTower.Controller;
using CubeTower.View;
using CubeTower.Model;

namespace CubeTower.Installers
{
    public class GameInstaller : MonoInstaller
    {
        [Header("Services")]
        [SerializeField] private ConfigService configService;
        [SerializeField] private SaveService saveService;

        [Header("Views")]
        [SerializeField] private BottomScrollView bottomScrollView;
        [SerializeField] private TowerView towerView;
        [SerializeField] private HoleView holeView;
        [SerializeField] private MessageView messageView;

        public override void InstallBindings()
        {
            Container.Bind<IConfigService>().FromInstance(configService).AsSingle();
            Container.Bind<ISaveService>().FromInstance(saveService).AsSingle();

            Container.Bind<GameModel>().AsSingle().NonLazy();

            Container.Bind<BottomScrollView>().FromInstance(bottomScrollView).AsSingle();
            Container.Bind<TowerView>().FromInstance(towerView).AsSingle();
            Container.Bind<HoleView>().FromInstance(holeView).AsSingle();
            Container.Bind<MessageView>().FromInstance(messageView).AsSingle();

            Container.BindInterfacesAndSelfTo<GameController>().AsSingle().NonLazy();
        }
    }
}
