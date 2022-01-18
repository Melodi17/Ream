using Ream.Lexing;

namespace Ream.Parsing
{
   public abstract class Stmt
   {
     public abstract T Accept<T>(Visitor<T> visitor);
     public interface Visitor<T>
     {
         public T VisitBlockStmt(Block stmt);
         public T VisitExpressionStmt(Expression stmt);
         public T VisitIfStmt(If stmt);
         public T VisitWriteStmt(Write stmt);
         public T VisitGlobalStmt(Global stmt);
         public T VisitLocalStmt(Local stmt);
         public T VisitWhileStmt(While stmt);
         public T VisitForStmt(For stmt);
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

     public class Write : Stmt
      {
     public readonly Expr expression;

         public Write(Expr expression)
          {
             this.expression = expression;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitWriteStmt(this);
          }
      }

     public class Global : Stmt
      {
     public readonly Token name;
     public readonly Expr initializer;

         public Global(Token name, Expr initializer)
          {
             this.name = name;
             this.initializer = initializer;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitGlobalStmt(this);
          }
      }

     public class Local : Stmt
      {
     public readonly Token name;
     public readonly Expr initializer;

         public Local(Token name, Expr initializer)
          {
             this.name = name;
             this.initializer = initializer;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitLocalStmt(this);
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

  }
}
