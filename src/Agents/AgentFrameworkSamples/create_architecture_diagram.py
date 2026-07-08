import matplotlib.pyplot as plt
import matplotlib.patches as patches
from matplotlib.patches import FancyBboxPatch, FancyArrowPatch
import matplotlib.lines as mlines

# Create figure with transparent background
fig, ax = plt.subplots(1, 1, figsize=(16, 14))
fig.patch.set_alpha(0.0)
ax.set_xlim(0, 10)
ax.set_ylim(0, 14)
ax.axis('off')

# Color scheme
color_user = '#4A90E2'
color_intent = '#E85D75'
color_plan = '#F39C12'
color_task = '#27AE60'
color_tools = '#9B59B6'
color_arrow = '#34495E'
color_text = '#2C3E50'

# Define positions
user_x, user_y = 1, 13
intent_x, intent_y = 1, 10.5
plan_x, plan_y = 5, 10.5
task_x, task_y = 9, 10.5

# Helper function to draw agent boxes with instructions
def draw_agent_box(ax, x, y, width, height, color, title, instructions, fontsize=8):
    # Draw box
    box = FancyBboxPatch((x - width/2, y - height/2), width, height,
                          boxstyle="round,pad=0.1", 
                          edgecolor=color, facecolor=color, 
                          alpha=0.15, linewidth=2.5)
    ax.add_patch(box)

    # Draw title
    ax.text(x, y + height/2 - 0.25, title, 
            ha='center', va='top', fontsize=12, fontweight='bold', color=color)

    # Draw instructions (wrapped)
    lines = instructions.split('\n')
    y_offset = y + height/2 - 0.6
    for line in lines:
        ax.text(x, y_offset, line, 
                ha='center', va='top', fontsize=fontsize, color=color_text, style='italic')
        y_offset -= 0.25

# Helper function to draw curved arrows
def draw_arrow(ax, x1, y1, x2, y2, label, color, curvature=0.3, label_offset=0.2):
    arrow = FancyArrowPatch((x1, y1), (x2, y2),
                           connectionstyle=f"arc3,rad={curvature}",
                           arrowstyle='->,head_width=0.4,head_length=0.4',
                           color=color, linewidth=2.5, alpha=0.8)
    ax.add_patch(arrow)

    # Add label
    mid_x, mid_y = (x1 + x2) / 2, (y1 + y2) / 2
    ax.text(mid_x, mid_y + label_offset, label,
            ha='center', va='bottom', fontsize=9, 
            bbox=dict(boxstyle='round,pad=0.4', facecolor='white', edgecolor=color, alpha=0.9),
            color=color_text, fontweight='bold')

# Draw title
ax.text(5, 13.5, 'CLAW Architecture: Three-Agent Workflow System',
        ha='center', va='center', fontsize=18, fontweight='bold', color=color_text)

# Draw User
user_box = FancyBboxPatch((user_x - 0.5, user_y - 0.3), 1, 0.6,
                          boxstyle="round,pad=0.05", 
                          edgecolor=color_user, facecolor=color_user, 
                          alpha=0.3, linewidth=2)
ax.add_patch(user_box)
ax.text(user_x, user_y, '👤 User', ha='center', va='center', 
        fontsize=11, fontweight='bold', color=color_user)

# Draw Intent Agent
intent_instructions = "• Analyze user intent\n• Decompose into plan steps\n• Call CreateAndExecutePlan\n• Each step: instructions, description, type"
draw_agent_box(ax, intent_x, intent_y, 2.5, 2.2, color_intent, 
               '🧠 Intent Agent\n(Decompose)', intent_instructions, fontsize=7)

# Draw Plan Agent
plan_instructions = "• Receive plan (list of steps)\n• Call ExecutePlan once\n• Orchestrate Task Agent\n• Summarize results"
draw_agent_box(ax, plan_x, plan_y, 2.5, 2.2, color_plan, 
               '⚙️ Plan Agent\n(Orchestrate)', plan_instructions, fontsize=7)

# Draw Task Agent
task_instructions = "• Execute individual tasks\n• Use CLI, Playwright, MS Learn\n• Return clear results\n• Explain errors if any"
draw_agent_box(ax, task_x, task_y, 2.5, 2.2, color_task, 
               '🔨 Task Agent\n(Execute)', task_instructions, fontsize=7)

# Draw Tool Box
tools_y = 8
tools_box = FancyBboxPatch((task_x - 1.2, tools_y - 0.8), 2.4, 1.6,
                           boxstyle="round,pad=0.1", 
                           edgecolor=color_tools, facecolor=color_tools, 
                           alpha=0.1, linewidth=2, linestyle='dashed')
ax.add_patch(tools_box)
ax.text(task_x, tools_y + 0.5, '🧰 Available Tools', 
        ha='center', va='center', fontsize=10, fontweight='bold', color=color_tools)
ax.text(task_x, tools_y + 0.2, '• ExecuteCliCommand', ha='center', va='center', fontsize=8, color=color_tools)
ax.text(task_x, tools_y - 0.05, '• Playwright (browser)', ha='center', va='center', fontsize=8, color=color_tools)
ax.text(task_x, tools_y - 0.3, '• MS Learn (docs)', ha='center', va='center', fontsize=8, color=color_tools)
ax.text(task_x, tools_y - 0.55, '• RememberKey / Print', ha='center', va='center', fontsize=8, color=color_tools)

# Draw sequence flow arrows
# 1. User to Intent Agent
draw_arrow(ax, user_x, user_y - 0.4, intent_x, intent_y + 1.1, 
           '1. User Prompt', color_user, curvature=0, label_offset=0.15)

# 2. Intent Agent to Plan Agent
draw_arrow(ax, intent_x + 1.25, intent_y + 0.3, plan_x - 1.25, plan_y + 0.3,
           '2. PlanStep[]', color_intent, curvature=0.2, label_offset=0.2)

# 3. Plan Agent to Task Agent (multiple times)
draw_arrow(ax, plan_x + 1.25, plan_y, task_x - 1.25, task_y,
           '3. Execute per step', color_plan, curvature=0.2, label_offset=0.2)

# 4. Task Agent to Tools
draw_arrow(ax, task_x, task_y - 1.1, task_x, tools_y + 0.8,
           '4. Tool calls', color_task, curvature=0, label_offset=0.15)

# 5. Task Agent returns to Plan Agent
draw_arrow(ax, task_x - 1.25, task_y - 0.3, plan_x + 1.25, plan_y - 0.3,
           '5. Result + context', color_task, curvature=-0.2, label_offset=-0.3)

# 6. Plan Agent returns to Intent Agent
draw_arrow(ax, plan_x - 1.25, plan_y - 0.6, intent_x + 1.25, intent_y - 0.6,
           '6. All results', color_plan, curvature=-0.2, label_offset=-0.3)

# 7. Intent Agent returns to User
draw_arrow(ax, intent_x, intent_y - 1.1, user_x, user_y - 0.4,
           '7. Summary', color_intent, curvature=0, label_offset=-0.3)

# Draw workflow alternative path
workflow_y = 6.5
ax.text(5, workflow_y + 0.8, '📋 Alternative: CLI-Only Workflow Path', 
        ha='center', va='center', fontsize=11, fontweight='bold', color='#E67E22')

workflow_box = FancyBboxPatch((3.5, workflow_y - 0.5), 3, 1,
                              boxstyle="round,pad=0.1", 
                              edgecolor='#E67E22', facecolor='#E67E22', 
                              alpha=0.1, linewidth=2, linestyle='dotted')
ax.add_patch(workflow_box)

ax.text(5, workflow_y + 0.3, 'Plan Agent can call RunCommandLineAsync', 
        ha='center', va='center', fontsize=8, color=color_text, style='italic')
ax.text(5, workflow_y, 'Builds Agent Framework Workflow', 
        ha='center', va='center', fontsize=8, color=color_text, style='italic')
ax.text(5, workflow_y - 0.3, 'One CommandExecutor per CLI step', 
        ha='center', va='center', fontsize=8, color=color_text, style='italic')

# Draw dashed line from Plan Agent to Workflow
workflow_arrow = FancyArrowPatch((plan_x, plan_y - 1.1), (5, workflow_y + 0.5),
                                connectionstyle="arc3,rad=0.1",
                                arrowstyle='->,head_width=0.3,head_length=0.3',
                                color='#E67E22', linewidth=2, alpha=0.6, linestyle='dotted')
ax.add_patch(workflow_arrow)

# Add key features box
features_y = 4.5
features_box = FancyBboxPatch((0.2, features_y - 1.5), 9.6, 1.5,
                              boxstyle="round,pad=0.1", 
                              edgecolor='#34495E', facecolor='#ECF0F1', 
                              alpha=0.3, linewidth=2)
ax.add_patch(features_box)

ax.text(5, features_y + 0.4, '🔑 Key Features', 
        ha='center', va='center', fontsize=11, fontweight='bold', color=color_text)

feature_items = [
    '✓ User Approval: Each step/command requires [Y]es/[S]kip/[A]bort/[U]nattended confirmation',
    '✓ Context Passing: Results from previous steps passed to subsequent tasks',
    '✓ MCP Integration: Playwright (browser) & MS Learn (docs) via Model Context Protocol',
    '✓ Flexible Execution: Direct tool calls OR Agent Framework Workflow for CLI-only plans',
    '✓ Memory Store: RememberKey/Print for persistent key-value storage across tasks'
]

y_pos = features_y
for item in feature_items:
    ax.text(5, y_pos, item, ha='center', va='center', fontsize=7.5, color=color_text)
    y_pos -= 0.25

# Add data flow annotations
data_y = 2.5
ax.text(5, data_y + 0.5, '📊 Data Flow Summary', 
        ha='center', va='center', fontsize=11, fontweight='bold', color=color_text)

flow_text = [
    'Intent Agent: User prompt → PlanStep[] (instructions, description, type)',
    'Plan Agent: PlanStep[] → Iterates → Task-specific prompts',
    'Task Agent: Task prompt + context → Tool execution → Result string',
    'Return Path: Results accumulate and flow back with context'
]

y_pos = data_y + 0.2
for text in flow_text:
    ax.text(5, y_pos, text, ha='center', va='center', fontsize=7.5, 
            color=color_text, style='italic')
    y_pos -= 0.25

# Add orchestrator note
ax.text(5, 0.8, 'IntentOrchestrator & PlanOrchestrator: Bridge agents via tool functions',
        ha='center', va='center', fontsize=8, color='#7F8C8D', 
        bbox=dict(boxstyle='round,pad=0.5', facecolor='white', edgecolor='#95A5A6', alpha=0.8))

# Add timestamp/signature
ax.text(5, 0.3, 'CLAW (Command Line Agent Workflow) - Three-Agent Architecture',
        ha='center', va='center', fontsize=9, color='#95A5A6', style='italic')

# Save with transparent background
plt.tight_layout()
plt.savefig('CLAW_Architecture_Diagram.png', dpi=300, bbox_inches='tight', 
            transparent=True, facecolor='none', edgecolor='none')
print("✅ Diagram saved as 'CLAW_Architecture_Diagram.png'")
plt.close()
