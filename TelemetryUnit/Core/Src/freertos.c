/* USER CODE BEGIN Header */
/**
  ******************************************************************************
  * File Name          : freertos.c
  * Description        : Code for freertos applications
  ******************************************************************************
  * @attention
  *
  * Copyright (c) 2026 STMicroelectronics.
  * All rights reserved.
  *
  * This software is licensed under terms that can be found in the LICENSE file
  * in the root directory of this software component.
  * If no LICENSE file comes with this software, it is provided AS-IS.
  *
  ******************************************************************************
  */
/* USER CODE END Header */

/* Includes ------------------------------------------------------------------*/
#include "FreeRTOS.h"
#include "task.h"
#include "main.h"
#include "cmsis_os.h"

/* Private includes ----------------------------------------------------------*/
/* USER CODE BEGIN Includes */
#include "../../Platform/SIM7600EH/sim7600eh_interface.h"
#include "../../Platform/GetDeviceId/get_device_id.h"
//#include <string.h>
#include "../../App/Network/interface.h"
#include "../../App/Gnss/interface.h"
#include "usart.h"
/* USER CODE END Includes */

/* Private typedef -----------------------------------------------------------*/
typedef StaticSemaphore_t osStaticMutexDef_t;
/* USER CODE BEGIN PTD */

/* USER CODE END PTD */

/* Private define ------------------------------------------------------------*/
/* USER CODE BEGIN PD */

/* USER CODE END PD */

/* Private macro -------------------------------------------------------------*/
/* USER CODE BEGIN PM */

/* USER CODE END PM */

/* Private variables ---------------------------------------------------------*/
/* USER CODE BEGIN Variables */

volatile bool led_state = false;
volatile bool armed = false;
volatile bool last_pin = true;

/* USER CODE END Variables */
/* Definitions for defaultTask */
osThreadId_t defaultTaskHandle;
const osThreadAttr_t defaultTask_attributes = {
  .name = "defaultTask",
  .stack_size = 128 * 4,
  .priority = (osPriority_t) osPriorityHigh1,
};
/* Definitions for CommandHandler */
osThreadId_t CommandHandlerHandle;
const osThreadAttr_t CommandHandler_attributes = {
  .name = "CommandHandler",
  .stack_size = 128 * 4,
  .priority = (osPriority_t) osPriorityAboveNormal,
};
/* Definitions for GnssFSM */
osThreadId_t GnssFSMHandle;
const osThreadAttr_t GnssFSM_attributes = {
  .name = "GnssFSM",
  .stack_size = 128 * 4,
  .priority = (osPriority_t) osPriorityNormal,
};
/* Definitions for NetworkFsm */
osThreadId_t NetworkFsmHandle;
const osThreadAttr_t NetworkFsm_attributes = {
  .name = "NetworkFsm",
  .stack_size = 256 * 4,
  .priority = (osPriority_t) osPriorityHigh,
};
/* Definitions for huart_sim_mutex */
osMutexId_t huart_sim_mutexHandle;
osStaticMutexDef_t huart_sim_mutexControlBlock;
const osMutexAttr_t huart_sim_mutex_attributes = {
  .name = "huart_sim_mutex",
  .cb_mem = &huart_sim_mutexControlBlock,
  .cb_size = sizeof(huart_sim_mutexControlBlock),
};
/* Definitions for huart_debug_mutex */
osMutexId_t huart_debug_mutexHandle;
osStaticMutexDef_t huart_debug_mutexControlBlock;
const osMutexAttr_t huart_debug_mutex_attributes = {
  .name = "huart_debug_mutex",
  .cb_mem = &huart_debug_mutexControlBlock,
  .cb_size = sizeof(huart_debug_mutexControlBlock),
};

/* Private function prototypes -----------------------------------------------*/
/* USER CODE BEGIN FunctionPrototypes */

/* USER CODE END FunctionPrototypes */

void StartDefaultTask(void *argument);
void start_command_handler(void *argument);
void start_gnss_fsm(void *argument);
void start_network_fsm(void *argument);

void MX_FREERTOS_Init(void); /* (MISRA C 2004 rule 8.1) */

/**
  * @brief  FreeRTOS initialization
  * @param  None
  * @retval None
  */
void MX_FREERTOS_Init(void) {
  /* USER CODE BEGIN Init */
  
  /* USER CODE END Init */
  /* Create the mutex(es) */
  /* creation of huart_sim_mutex */
  huart_sim_mutexHandle = osMutexNew(&huart_sim_mutex_attributes);

  /* creation of huart_debug_mutex */
  huart_debug_mutexHandle = osMutexNew(&huart_debug_mutex_attributes);

  /* USER CODE BEGIN RTOS_MUTEX */
  /* add mutexes, ... */
  /* USER CODE END RTOS_MUTEX */

  /* USER CODE BEGIN RTOS_SEMAPHORES */
  /* add semaphores, ... */
  /* USER CODE END RTOS_SEMAPHORES */

  /* USER CODE BEGIN RTOS_TIMERS */
  /* start timers, add new ones, ... */
  /* USER CODE END RTOS_TIMERS */

  /* USER CODE BEGIN RTOS_QUEUES */
  /* add queues, ... */
  /* USER CODE END RTOS_QUEUES */

  /* Create the thread(s) */
  /* creation of defaultTask */
  defaultTaskHandle = osThreadNew(StartDefaultTask, NULL, &defaultTask_attributes);

  /* creation of CommandHandler */
  CommandHandlerHandle = osThreadNew(start_command_handler, NULL, &CommandHandler_attributes);

  /* creation of GnssFSM */
  GnssFSMHandle = osThreadNew(start_gnss_fsm, NULL, &GnssFSM_attributes);

  /* creation of NetworkFsm */
  NetworkFsmHandle = osThreadNew(start_network_fsm, NULL, &NetworkFsm_attributes);

  /* USER CODE BEGIN RTOS_THREADS */
  /* add threads, ... */
  /* USER CODE END RTOS_THREADS */

  /* USER CODE BEGIN RTOS_EVENTS */
  /* add events, ... */
  /* USER CODE END RTOS_EVENTS */

}

/* USER CODE BEGIN Header_StartDefaultTask */
/**
  * @brief  Function implementing the defaultTask thread.
  * @param  argument: Not used
  * @retval None
  */
/* USER CODE END Header_StartDefaultTask */
void StartDefaultTask(void *argument)
{
  /* USER CODE BEGIN StartDefaultTask */
  /* Infinite loop */
  bool armed = false;
  bool last_pin = true;

for (;;)
{
    bool pin = HAL_GPIO_ReadPin(UserButton_GPIO_Port, UserButton_Pin);

    if (pin == PIN_STATE_LOW)
    {
        armed = true;
    }

    if (armed && last_pin == PIN_STATE_LOW && pin == PIN_STATE_HIGH)
    {
        HAL_GPIO_TogglePin(LD2_GPIO_Port, LD2_Pin);
        armed = false;
        led_state = !led_state;
    }

    last_pin = pin;

    osDelay(1);
}
  /* USER CODE END StartDefaultTask */
}

/* USER CODE BEGIN Header_start_command_handler */
/**
* @brief Function implementing the CommandHandler thread.
* @param argument: Not used
* @retval None
*/
/* USER CODE END Header_start_command_handler */
void start_command_handler(void *argument)
{
  /* USER CODE BEGIN start_command_handler */
  /* Infinite loop */
  for(;;)
  {
    //if (led_state)
    //{
      check_for_device_id_request_command(&huart2);
    //}
    osDelay(1);
  }
  /* USER CODE END start_command_handler */
}

/* USER CODE BEGIN Header_start_gnss_fsm */
/**
* @brief Function implementing the GnssFSM thread.
* @param argument: Not used
* @retval None
*/
/* USER CODE END Header_start_gnss_fsm */
void start_gnss_fsm(void *argument)
{
  /* USER CODE BEGIN start_gnss_fsm */
  /* Infinite loop */
  for(;;)
  {
    osDelay(1);
  }
  /* USER CODE END start_gnss_fsm */
}

/* USER CODE BEGIN Header_start_network_fsm */
/**
* @brief Function implementing the NetworkFsm thread.
* @param argument: Not used
* @retval None
*/
/* USER CODE END Header_start_network_fsm */
void start_network_fsm(void *argument)
{
  /* USER CODE BEGIN start_network_fsm */
  
  /* Infinite loop */
  for(;;)
  {
    if (!led_state)
    {
      gnss_fsm_step();
      network_fsm_step();
    }
    //HAL_GPIO_TogglePin(LD2_GPIO_Port, LD2_Pin);
    osDelay(1000);
  }
  /* USER CODE END start_network_fsm */
}

/* Private application code --------------------------------------------------*/
/* USER CODE BEGIN Application */

/* USER CODE END Application */

