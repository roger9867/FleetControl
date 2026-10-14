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
#include "../../Platform/GetDeviceId/get_device_id.h"
#include "../../App/Network/interface.h"
#include "../../App/Gnss/interface.h"
#include "../../App/Telemetry/interface.h"
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
/* Definitions for FsmTask */
osThreadId_t FsmTaskHandle;
const osThreadAttr_t FsmTask_attributes = {
  .name = "FsmTask",
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
void start_fsm_task(void *argument);

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

  /* creation of FsmTask */
  FsmTaskHandle = osThreadNew(start_fsm_task, NULL, &FsmTask_attributes);

  /* USER CODE BEGIN RTOS_THREADS */
  /* add threads, ... */
  /* USER CODE END RTOS_THREADS */

  /* USER CODE BEGIN RTOS_EVENTS */
  /* add events, ... */
  /* USER CODE END RTOS_EVENTS */

}

/* USER CODE BEGIN Header_StartDefaultTask */
/**
  * @brief  Default task, switches the operating modes: command output / normal operation
  * @param  argument: Not used
  * @retval None
  */
/* USER CODE END Header_StartDefaultTask */
void StartDefaultTask(void *argument)
{
  /* USER CODE BEGIN StartDefaultTask */
  /* Infinite loop */
  const uint32_t DEBOUNCE_MS = 30;

  bool stable_pin = HAL_GPIO_ReadPin(UserButton_GPIO_Port, UserButton_Pin);
  bool last_reading = stable_pin;
  uint32_t last_change = HAL_GetTick();

for (;;)
{
    bool pin = HAL_GPIO_ReadPin(UserButton_GPIO_Port, UserButton_Pin);

    if (pin != last_reading)
    {
        last_reading = pin;
        last_change = HAL_GetTick();
    }
    else if ((HAL_GetTick() - last_change) >= DEBOUNCE_MS && pin != stable_pin)
    {
        // Confirmed, debounced edge: only toggle on release (LOW -> HIGH)
        if (stable_pin == PIN_STATE_LOW && pin == PIN_STATE_HIGH)
        {
            HAL_GPIO_TogglePin(LD2_GPIO_Port, LD2_Pin);
            led_state = !led_state;
        }

        stable_pin = pin;
    }

    osDelay(1);
}
  /* USER CODE END StartDefaultTask */
}

/* USER CODE BEGIN Header_start_command_handler */
/**
* @brief Implements the command handler task.
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
    if (led_state)
    {
      check_for_device_id_request_command();
    }
    osDelay(1);
  }
  /* USER CODE END start_command_handler */
}

/* USER CODE BEGIN Header_start_fsm_task */
/**
* @brief Runs the FsmTask thread. Ticks the GNSS, network and
*        TelemetryFsm in this order (GNSS/Network must be ready
*        before Telemetry can publish).
* @param argument: Not used
* @retval None
*/
/* USER CODE END Header_start_fsm_task */
void start_fsm_task(void *argument)
{
  /* USER CODE BEGIN start_fsm_task */

  /* Infinite loop */
  for(;;)
  {
    if (!led_state)
    {
      gnss_fsm_step();
      network_fsm_step();
      telemetry_fsm_step();
    }
    osDelay(1000);
  }
  /* USER CODE END start_fsm_task */
}

/* Private application code --------------------------------------------------*/
/* USER CODE BEGIN Application */

/* USER CODE END Application */

