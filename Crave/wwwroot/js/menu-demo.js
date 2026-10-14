/* Copia de EJEMPLO de la carta, solo para el modo demo (MODO_DEMO = true en app.js).
   Tiene la misma forma que devuelve el servidor en /api/menu.
   La carta real se edita en Data/MenuData.cs. */
const MENU_DEMO = {
  "categorias": [
    {
      "id": "pollo",
      "nombre": "Pollo frito",
      "nota": "Combos y baldes",
      "icono": "pollo",
      "productos": [
        {
          "id": "p2",
          "nombre": "Combo 2 piezas",
          "descripcion": "2 piezas de pollo frito, papas y una salsa.",
          "precio": 7900,
          "icono": "pollo",
          "etiqueta": "Para 1",
          "destacado": true,
          "imagen": "",
          "agotado": false
        },
        {
          "id": "p4",
          "nombre": "Combo 4 piezas",
          "descripcion": "4 piezas de pollo frito, papas y dos salsas.",
          "precio": 13900,
          "icono": "pollo",
          "etiqueta": "Para 2",
          "destacado": true,
          "imagen": "",
          "agotado": false
        },
        {
          "id": "p8",
          "nombre": "Balde 8 piezas",
          "descripcion": "8 piezas para compartir, papas grandes y tres salsas.",
          "precio": 24500,
          "icono": "balde",
          "etiqueta": "Para 4",
          "destacado": true,
          "imagen": "",
          "agotado": false
        },
        {
          "id": "tend",
          "nombre": "Tenders x6",
          "descripcion": "6 tiras de pechuga crocante con salsa a elección.",
          "precio": 8900,
          "icono": "pollo",
          "agotado": true,
          "imagen": "",
          "etiqueta": "",
          "destacado": false
        }
      ]
    },
    {
      "id": "tacos",
      "nombre": "Tacos",
      "nota": "Por unidad o en combo",
      "icono": "taco",
      "productos": [
        {
          "id": "tp",
          "nombre": "Taco de pollo crispy",
          "descripcion": "Pollo frito, repollo y salsa.",
          "precio": 3600,
          "icono": "taco",
          "imagen": "",
          "etiqueta": "",
          "agotado": false,
          "destacado": false
        },
        {
          "id": "tc",
          "nombre": "Taco de carne",
          "descripcion": "Carne, cebolla, cilantro y salsa.",
          "precio": 3800,
          "icono": "taco",
          "imagen": "",
          "etiqueta": "",
          "agotado": false,
          "destacado": false
        },
        {
          "id": "t3",
          "nombre": "Combo 3 tacos",
          "descripcion": "3 tacos a elección y papas.",
          "precio": 10900,
          "icono": "taco",
          "etiqueta": "Para 1",
          "destacado": true,
          "imagen": "",
          "agotado": false
        },
        {
          "id": "t6",
          "nombre": "Combo 6 tacos",
          "descripcion": "6 tacos a elección y papas grandes.",
          "precio": 19900,
          "icono": "taco",
          "etiqueta": "Para 2",
          "destacado": true,
          "imagen": "",
          "agotado": false
        }
      ]
    },
    {
      "id": "extras",
      "nombre": "Extras y bebidas",
      "nota": "Para acompañar",
      "icono": "papas",
      "productos": [
        {
          "id": "pa",
          "nombre": "Papas fritas",
          "descripcion": "Porción individual.",
          "precio": 3500,
          "icono": "papas",
          "imagen": "",
          "etiqueta": "",
          "agotado": false,
          "destacado": false
        },
        {
          "id": "sa",
          "nombre": "Salsa extra",
          "descripcion": "Pedí la que quieras.",
          "precio": 600,
          "icono": "salsa",
          "imagen": "",
          "etiqueta": "",
          "agotado": false,
          "destacado": false
        },
        {
          "id": "ga",
          "nombre": "Gaseosa 500 ml",
          "descripcion": "Línea a elección.",
          "precio": 2200,
          "icono": "gaseosa",
          "imagen": "",
          "etiqueta": "",
          "agotado": false,
          "destacado": false
        },
        {
          "id": "ag",
          "nombre": "Agua 500 ml",
          "descripcion": "Con o sin gas.",
          "precio": 1800,
          "icono": "gaseosa",
          "imagen": "",
          "etiqueta": "",
          "agotado": false,
          "destacado": false
        }
      ]
    }
  ],
  "zonas": [
    {
      "id": "z1",
      "nombre": "Zona 1",
      "costo": 1000
    },
    {
      "id": "z2",
      "nombre": "Zona 2",
      "costo": 1800
    }
  ]
};
