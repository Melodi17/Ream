using Ream.Lexing;
using Ream.Interpreting;
using Ream.SDK;

namespace Ream.Parsing
{
   public abstract class Stmt
   {
     public abstract T Accept<T>(Visitor<T> visitor);
     public interface Visitor<T>
     {
         public T VisitBlockStmt(Block stmt);
         public T VisitClassStmt(Class stmt);
         public T VisitExpressionStmt(Expression stmt);
         public T VisitFunctionStmt(Function stmt);
         public T VisitIfStmt(If stmt);
         public T VisitThreadStmt(Thread stmt);
         public T VisitPrintStmt(Print stmt);
         public T VisitReturnStmt(Return stmt);
         public T VisitTypedStmt(Typed stmt);
         public T VisitWhileStmt(While stmt);
         public T VisitImportStmt(Import stmt);
         public T VisitForStmt(For stmt);
         public T VisitEvaluateStmt(Evaluate stmt);
     }
     public class Block : Stmt
      {
     public readonly List<Stmt> statements;

         public Block(List<Stmt> statements)
          {
             this.statements = statements;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitBlockStmt(this);
          }
      }

     public class Class : Stmt
      {
     public readonly Token name;
     public readonly List<Stmt.Function> functions;

         public Class(Token name, List<Stmt.Function> functions)
          {
             this.name = name;
             this.functions = functions;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitClassStmt(this);
          }
      }

     public class Expression : Stmt
      {
     public readonly Expr expression;

         public Expression(Expr expression)
          {
             this.expression = expression;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitExpressionStmt(this);
          }
      }

     public class Function : Stmt
      {
     public readonly Token name;
     public readonly VariableType type;
     public readonly List<Token> parameters;
     public readonly List<Stmt> body;

         public Function(Token name, VariableType type, List<Token> parameters, List<Stmt> body)
          {
             this.name = name;
             this.type = type;
             this.parameters = parameters;
             this.body = body;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitFunctionStmt(this);
          }
      }

     public class If : Stmt
      {
     public readonly Expr condition;
     public readonly Stmt thenBranch;
     public readonly Stmt elseBranch;

         public If(Expr condition, Stmt thenBranch, Stmt elseBranch)
          {
             this.condition = condition;
             this.thenBranch = thenBranch;
             this.elseBranch = elseBranch;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitIfStmt(this);
          }
      }

     public class Thread : Stmt
      {
     public readonly Stmt body;

         public Thread(Stmt body)
          {
             this.body = body;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitThreadStmt(this);
          }
      }

     public class Print : Stmt
      {
     public readonly Expr expression;

         public Print(Expr expression)
          {
             this.expression = expression;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitPrintStmt(this);
          }
      }

     public class Return : Stmt
      {
     public readonly Token keyword;
     public readonly Expr value;

         public Return(Token keyword, Expr value)
          {
             this.keyword = keyword;
             this.value = value;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitReturnStmt(this);
          }
      }

     public class Typed : Stmt
      {
     public readonly Token name;
     public readonly Expr initializer;
     public readonly VariableType type;

         public Typed(Token name, Expr initializer, VariableType type)
          {
             this.name = name;
             this.initializer = initializer;
             this.type = type;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitTypedStmt(this);
          }
      }

     public class While : Stmt
      {
     public readonly Expr condition;
     public readonly Stmt body;

         public While(Expr condition, Stmt body)
          {
             this.condition = condition;
             this.body = body;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitWhileStmt(this);
          }
      }

     public class Import : Stmt
      {
     public readonly Token name;

         public Import(Token name)
          {
             this.name = name;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitImportStmt(this);
          }
      }

     public class For : Stmt
      {
     public readonly Token name;
     public readonly Expr iterator;
     public readonly Stmt body;

         public For(Token name, Expr iterator, Stmt body)
          {
             this.name = name;
             this.iterator = iterator;
             this.body = body;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitForStmt(this);
          }
      }

     public class Evaluate : Stmt
      {
     public readonly Expr value;

         public Evaluate(Expr value)
          {
             this.value = value;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitEvaluateStmt(this);
          }
      }

  }
}
